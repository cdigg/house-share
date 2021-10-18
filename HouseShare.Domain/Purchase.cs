//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HouseShare.Domain
{
    using System;
    using System.Collections.Generic;
    
    public partial class Purchase
    {
        public Purchase()
        {
            this.MoneyTransactions = new HashSet<MoneyTransaction>();
            this.PurchaseShares = new HashSet<PurchaseShare>();
        }
    
        public int Id { get; set; }
        public System.DateTime PurchaseDate { get; set; }
        public decimal Amount { get; set; }
        public bool Split { get; set; }
        public Nullable<System.DateTime> FromDate { get; set; }
        public Nullable<System.DateTime> ToDate { get; set; }
        public string Note { get; set; }
        public string Category { get; set; }
    
        public virtual House House { get; set; }
        public virtual ICollection<MoneyTransaction> MoneyTransactions { get; set; }
        public virtual ShareEntity ShareEntity { get; set; }
        public virtual ICollection<PurchaseShare> PurchaseShares { get; set; }
    }
}
