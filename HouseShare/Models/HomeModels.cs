using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace HouseShare.Models
{
    public class ShareEntityVM
    {
        public int? Id { get; set; }
        public string Name { get; set; }
       
        public bool Current { get; set; }
        public IEnumerable<ShareDateVM> Dates { get; set; }
    }

    public class ShareDateVM
    {
        public int? Id { get; set; }
        [XmlIgnore]
        public DateTime From { get; set; }
        [XmlIgnore]
        public DateTime? To { get; set; }
        public string FromStr { get; set; }
        public string ToStr { get; set; }
        public decimal Shares { get; set; }
    }

    public class TxVM
    {
        public string Type { get;set; }
        public string PaidDate {get;set;}
        public string Category {get;set;}
        public string Desc {get;set;}
        public string PType {get; set; }
        public string SplitFrom {get;set;}
        public string SplitTo {get;set;}
        public int? EntityFrom {get;set;}
        public int? EntityTo {get;set;}
        public decimal Amount { get; set; }
        public List<EntityShareVM> Shares { get; set; }
    }

    public class BalanceVM
    {
        public int FromId { get; set; }
        public int ToId { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public decimal Amount { get; set; }
    }

    public class MonthVM
    {
        public int Year { get; set; }
        public int Number {get;set;}
        public bool Loaded { get; set; }
        public string Name {get;set;}
        public decimal Food { get; set; }
        public decimal Utilities { get; set; }
        public decimal Misc { get; set; }
        public List<MonthTxVM> Transactions { get; set; }
    }

    public class MonthTxVM
    {
        public string Type { get; set; }
        public string Date { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; }
        public string Text { get; set; }
    }

    public class EntityShareVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Shares { get; set; }
    }
}