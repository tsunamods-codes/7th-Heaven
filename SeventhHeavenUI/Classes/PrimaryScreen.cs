using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeventhHeaven.Classes
{
    public class PrimaryScreen
    {

        public static List<SupportedResolution> GetSupportedResolutions()
        {
            var scope = new System.Management.ManagementScope();
            var q = new System.Management.ObjectQuery("SELECT * FROM CIM_VideoControllerResolution");

            UInt32 maxHResolution;
            UInt32 maxVResolution;

            using (var searcher = new System.Management.ManagementObjectSearcher(scope, q))
            {
                var results = searcher.Get();

                foreach (var item in results)
                {
                    maxHResolution = (UInt32)item["HorizontalResolution"];
                    maxVResolution = (UInt32)item["VerticalResolution"];
                    SupportedResolution.CreateIfNotExist(maxHResolution, maxVResolution);
                }
            }

            return SupportedResolution.GetAllCachedResolutions();
        }
    }
}
