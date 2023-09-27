using System;
using System.Collections.Generic;
using System.Text;

namespace SqlDependancyTest.Models.DTO
{
    public class IntegrationMessageDTO
    {
        public Guid Id { get; set; }
        public String Message { get; set; }
        public String BatchId { get; set; }
        public String ProductionUnitId { get; set; }
        public int ResendCounter { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime EntryOn { get; set; }
        public Boolean UseWebService { get; set; }
        public String FolderPath { get; set; }
        public String WebServiceAddress { get; set; }
        public String SystemName { get; set; }
        public string DataStoredProcedure { get; set; }
        public string MessageTemplate { get; set; }
        public int DefaultResendValue { get; set; }
        public String Description { get; set; }
    }
}
