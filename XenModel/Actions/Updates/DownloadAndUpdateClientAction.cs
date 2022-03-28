﻿/* Copyright (c) Citrix Systems, Inc. 
 * All rights reserved. 
 * 
 * Redistribution and use in source and binary forms, 
 * with or without modification, are permitted provided 
 * that the following conditions are met: 
 * 
 * *   Redistributions of source code must retain the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer. 
 * *   Redistributions in binary form must reproduce the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer in the documentation and/or other 
 *     materials provided with the distribution. 
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND 
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF 
 * SUCH DAMAGE.
 */

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using XenCenterLib;

namespace XenAdmin.Actions
{
    public class DownloadAndUpdateClientAction : AsyncAction, IByteProgressAction
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const int SLEEP_TIME_TO_CHECK_DOWNLOAD_STATUS_MS = 900;
        private const int SLEEP_TIME_BEFORE_RETRY_MS = 5000;

        //If you consider increasing this for any reason (I think 5 is already more than enough), have a look at the usage of SLEEP_TIME_BEFORE_RETRY_MS in DownloadFile() as well.
        private const int MAX_NUMBER_OF_TRIES = 5;

        private readonly Uri address;
        private readonly string outputPathAndFileName;
        private readonly string updateName;
        private readonly bool downloadUpdate;
        private DownloadState updateDownloadState;
        private Exception updateDownloadError;
        private string checksum;
        private WebClient client;

        public string ByteProgressDescription { get; set; }

        public DownloadAndUpdateClientAction(string updateName, Uri uri, string outputFileName, string checksum)
            : base(null, string.Format(Messages.DOWNLOAD_CLIENT_INSTALLER_ACTION_TITLE, updateName),
                string.Empty, true)
        {
            this.updateName = updateName;
            address = uri;
            downloadUpdate = address != null;
            this.outputPathAndFileName = outputFileName;
            this.checksum = checksum;
        }

        private void DownloadFile()
        {
            int errorCount = 0;
            bool needToRetry = false;

            client = new WebClient();
            client.DownloadProgressChanged += client_DownloadProgressChanged;
            client.DownloadFileCompleted += client_DownloadFileCompleted;

            // register event handler to detect changes in network connectivity
            NetworkChange.NetworkAvailabilityChanged += NetworkAvailabilityChanged;

            try
            {
                do
                {
                    if (needToRetry)
                        Thread.Sleep(SLEEP_TIME_BEFORE_RETRY_MS);

                    needToRetry = false;

                    client.Proxy = XenAdminConfigManager.Provider.GetProxyFromSettings(null, false);

                    //start the download
                    updateDownloadState = DownloadState.InProgress;
                    client.DownloadFileAsync(address, outputPathAndFileName);

                    bool updateDownloadCancelling = false;

                    //wait for the file to be downloaded
                    while (updateDownloadState == DownloadState.InProgress)
                    {
                        if (!updateDownloadCancelling && (Cancelling || Cancelled))
                        {
                            Description = Messages.DOWNLOAD_AND_EXTRACT_ACTION_DOWNLOAD_CANCELLED_DESC;
                            client.CancelAsync();
                            updateDownloadCancelling = true;
                        }

                        Thread.Sleep(SLEEP_TIME_TO_CHECK_DOWNLOAD_STATUS_MS);
                    }

                    if (updateDownloadState == DownloadState.Cancelled)
                        throw new CancelledException();

                    if (updateDownloadState == DownloadState.Error)
                    {
                        needToRetry = true;

                        // this many errors so far - including this one
                        errorCount++;

                        // logging only, it will retry again.
                        log.ErrorFormat(
                            "Error while downloading from '{0}'. Number of errors so far (including this): {1}. Trying maximum {2} times.",
                            address, errorCount, MAX_NUMBER_OF_TRIES);

                        if (updateDownloadError == null)
                            log.Error("An unknown error occurred.");
                        else
                            log.Error(updateDownloadError);
                    }
                } while (errorCount < MAX_NUMBER_OF_TRIES && needToRetry);
            }
            finally
            {
                client.DownloadProgressChanged -= client_DownloadProgressChanged;
                client.DownloadFileCompleted -= client_DownloadFileCompleted;

                NetworkChange.NetworkAvailabilityChanged -= NetworkAvailabilityChanged;

                client.Dispose();
            }

            //if this is still the case after having retried MAX_NUMBER_OF_TRIES number of times.
            if (updateDownloadState == DownloadState.Error)
            {
                log.ErrorFormat("Giving up - Maximum number of retries ({0}) has been reached.", MAX_NUMBER_OF_TRIES);
                throw updateDownloadError ?? new Exception(Messages.ERROR_UNKNOWN);
            }
        }

        private void NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            if (!e.IsAvailable && client != null && updateDownloadState == DownloadState.InProgress)
            {
                updateDownloadError = new WebException(Messages.NETWORK_CONNECTIVITY_ERROR);
                updateDownloadState = DownloadState.Error;
                client.CancelAsync();
            }
        }

        protected override void Run()
        {
            if (downloadUpdate)
            {
                log.InfoFormat("Downloading '{0}' installer (from '{1}') to '{2}'", updateName, address, outputPathAndFileName);
                Description = string.Format(Messages.DOWNLOAD_CLIENT_INSTALLER_ACTION_DESCRIPTION, updateName);
                LogDescriptionChanges = false;
                DownloadFile();
                LogDescriptionChanges = true;

                if (IsCompleted || Cancelled)
                    return;

                if (Cancelling)
                    throw new CancelledException();
            }

            ValidateMsi();

            if (!File.Exists(outputPathAndFileName))
                throw new Exception(Messages.DOWNLOAD_CLIENT_INSTALLER_MSI_NOT_FOUND);

            Description = Messages.COMPLETED;
        }

        private void ValidateMsi()
        {
            using (FileStream stream = new FileStream(outputPathAndFileName, FileMode.Open, FileAccess.Read))
            {
                var calculatedChecksum = string.Empty; 

                var hash = StreamUtilities.ComputeHash(stream, out _);
                if (hash != null)
                    calculatedChecksum = string.Join("", hash.Select(b => $"{b:x2}"));

                // Check if calculatedChecksum matches what is in chcupdates.xml
                if (!checksum.Equals(calculatedChecksum, StringComparison.InvariantCultureIgnoreCase))
                    throw new Exception(Messages.UPDATE_CLIENT_INVALID_CHECKSUM );
            }

            bool valid = false;
            try
            {
                // Check digital signature of .msi
                using (var basicSigner = X509Certificate.CreateFromSignedFile(outputPathAndFileName))
                {
                    using (var cert = new X509Certificate2(basicSigner))
                    {
                        valid = cert.Verify();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(Messages.UPDATE_CLIENT_FAILED_CERTIFICATE_CHECK, e);
            }
            

            if (!valid)
                throw new Exception(Messages.UPDATE_CLIENT_INVALID_DIGITAL_CERTIFICATE);
        }

        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            int pc = (int)(95.0 * e.BytesReceived / e.TotalBytesToReceive);
            var descr = string.Format(Messages.DOWNLOAD_CLIENT_INSTALLER_ACTION_PROGRESS_DESCRIPTION, updateName,
                                            Util.DiskSizeString(e.BytesReceived, "F1"),
                                            Util.DiskSizeString(e.TotalBytesToReceive));
            ByteProgressDescription = descr;
            Tick(pc, descr);
        }

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled && updateDownloadState == DownloadState.Error) // cancelled due to network connectivity issue (see NetworkAvailabilityChanged)
                return;

            if (e.Cancelled)
            {
                updateDownloadState = DownloadState.Cancelled;
                log.DebugFormat("Client update '{0}' download cancelled by the user", updateName);
                return;
            }

            if (e.Error != null)
            {
                updateDownloadError = e.Error;
                log.DebugFormat("Client update '{0}' download failed", updateName);
                updateDownloadState = DownloadState.Error;
                return;
            }

            updateDownloadState = DownloadState.Completed;
            log.DebugFormat("Client update '{0}' download completed successfully", updateName);
        }

        public override void RecomputeCanCancel()
        {
            CanCancel = !Cancelling && !IsCompleted && (updateDownloadState == DownloadState.InProgress);
        }
    }
}

