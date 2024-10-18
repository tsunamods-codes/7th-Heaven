using Iros.Workshop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore
{
    public enum DownloadCategory
    {
        Mod,
        Catalog,
        Image,
        AppUpdate,
        ModPatch
    }

    public class DownloadItem
    {
        public Guid UniqueId { get; set; }

        /// <summary>
        /// Id of the parent download (used for patches). null represents no parent.
        /// </summary>
        public Guid? ParentUniqueID { get; set; }

        public DownloadCategory Category { get; set; }

        public string SaveFilePath { get; set; }

        /// <summary>
        /// Callback procedure to perform when download completes.
        /// </summary>
        public Install.InstallProcedure IProc { get; set; }

        /// <summary>
        /// Instance of <see cref="FileDownloadTask"/> that performs download operation for url/gdrive downloads
        /// </summary>
        /// <remarks>
        /// This field will be null for mega.co.nz downloads and should be ignored
        /// </remarks>
        public FileDownloadTask FileDownloadTask { get; set; }
        public DateTime LastCalc { get; set; }
        public long LastBytes { get; set; }

        /// <summary>
        /// Action to contain custom logic for performing a cancel of the download
        /// </summary>
        public Action PerformCancel { get; set; }

        /// <summary>
        /// This should be called within <see cref="PerformCancel"/>
        /// </summary>
        public Action OnCancel { get; set; }

        /// <summary>
        /// Action to contain custom logic for handling an error during download
        /// </summary>
        public Action OnError { get; set; }

        public string ItemName { get; set; }
        public StringKey? ItemNameTranslationKey { get; set; }
        public double PercentComplete { get; set; }
        public string DownloadSpeed { get; set; }
        public string RemainingTime { get; set; }
        public List<string> Links { get; set; }
        public bool HasStarted { get; set; }

        public bool IsModOrPatchDownload
        {
            get
            {
                return Category == DownloadCategory.Mod || Category == DownloadCategory.ModPatch;
            }
        }

        /// <summary>
        /// The message to show to the user when downloading a mod that may user ExternalUrl links. If the mod download only has one link to ExternalUrl then the message will be changed to reflect that.
        /// </summary>
        public string ExternalUrlDownloadMessage { get; set; }
        public bool IsFileDownloadPaused
        {
            get
            {
                if (FileDownloadTask == null)
                    return false;

                return FileDownloadTask.IsPaused;
            }
        }

        public DownloadItem()
        {
            LastCalc = DateTime.Now;
            UniqueId = Guid.NewGuid();
            ParentUniqueID = null;
            PercentComplete = 0;
            Links = new List<string>();
            HasStarted = false;
            RemainingTime = "Unknown";
            DownloadSpeed = "Pending...";
            ExternalUrlDownloadMessage = "";
            ItemNameTranslationKey = null;
        }

    }
}
