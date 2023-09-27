using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace IntegrationService.Models
{
    class MaximoAssetData
    {
        public string filename;
        public int FileType = 0;

        public string AssetNum;
        public string SiteId;
        public string C_IsMes;
        public string C_MesType;
        public string IsRunning;
        public string OrgId;
        public string Parent;
        public string Status;

        public List<MaximoMeterData> MeterData = new List<MaximoMeterData>();
    }
}
