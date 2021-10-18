using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HouseShare.Domain;
using HouseShare.Domain.Repositories.Abstract;
using HouseShare.Models;
using HouseShare.Util;
using WebMatrix.WebData;

namespace HouseShare.Controllers
{
    
    public class HomeController : BaseController
    {
        //TODO: get this dynamically
        private const int _HOUSEID = 1;

        private IShareEntity _shareEntityRepository;
        private IShareEntityDate _shareEntityDateRepository;
        private IUserProfile _userProfileRepository;
        private IHouse _houseRepository;

        private IPayment _paymentRepository;
        private IOwe _oweRepository;
        private IPurchase _purchaseRepository;
        private IMoneyTransaction _trxRepository;

        private int houseId;

        public HomeController(IShareEntity shareEntityRepo,IUserProfile userProfileRepo,IHouse houseRepository,
            IPayment paymentRepo, IOwe oweRepo, IPurchase purchaseRepo, IMoneyTransaction trxRepo, IShareEntityDate shareEntityDateRepo)
        {
            _shareEntityRepository = shareEntityRepo;
            _shareEntityDateRepository = shareEntityDateRepo;
            _userProfileRepository = userProfileRepo;
            _houseRepository = houseRepository;
            _paymentRepository = paymentRepo;
            _oweRepository = oweRepo;
            _purchaseRepository = purchaseRepo;
            _trxRepository = trxRepo;

            if (WebSecurity.IsAuthenticated)
            {
                //WebSecurity.InitializeDatabaseConnection("DefaultConnection", "UserProfile", "UserId", "UserName", autoCreateTables: true);

                //houseId = _userProfileRepository.Find(WebSecurity.CurrentUserId).House.Id;
            }
        }

        public ActionResult Index()
        {
            if (!WebSecurity.IsAuthenticated)
                return RedirectToAction("Login", "Account");
            else
                return View();
        }

        public ActionResult LoadShares(string dte)
        {
            DateTime date = DateTime.Parse(dte);
            var list = _shareEntityDateRepository.Get.Where(x => x.FromDate <= date && (x.ToDate == null || x.ToDate >= date))
                .Select(x => new EntityShareVM
                {
                    Id = x.ShareEntity.Id,
                    Name = x.ShareEntity.Name,
                    Shares = x.Shares
                });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        //load entities
        public ActionResult LoadEntities()
        {
            DateTime now = DateTime.Now;
            var list = _shareEntityRepository.Get.Where(x => x.House.Id == _HOUSEID)
                .OrderBy(x => x.Name)
                .Select(x => new ShareEntityVM
                {
                    Id = x.Id,
                    Name = x.Name,
                    
                    Dates = x.ShareEntityDates.OrderBy(y => y.FromDate).Select(y => new ShareDateVM
                    {
                        Id= y.Id,
                        From = y.FromDate,
                        To = y.ToDate,
                        Shares = y.Shares
                    }),
                    Current = x.ShareEntityDates.Any(y => y.FromDate <= now && (y.ToDate == null || y.ToDate >= now))
                }).ToList();

            foreach (var e in list)
            {
                foreach (var z in e.Dates)
                {
                    z.FromStr = z.From.ToString("MM/dd/yyyy");
                    z.ToStr = z.To.HasValue ? z.To.Value.ToString("MM/dd/yyyy") : "";
                }
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RecalcAll()
        {
            //cycle through all Owes and apply them as needed to MoneyTransactions
            foreach (Owe o in _oweRepository.Get)
            {
                
            }


            return Json("",JsonRequestBehavior.AllowGet);
        }

        public ActionResult SaveShareEntity(ShareEntityVM model)
        {
            ShareEntity se = null;
            if (model.Id.HasValue)
                se = _shareEntityRepository.Find(model.Id);
            else
            {
                se = new ShareEntity
                {
                    House = _houseRepository.Find(_HOUSEID)
                };
                _shareEntityRepository.Add(se);
            }

            se.Name = model.Name;

            foreach (ShareDateVM sdv in model.Dates)
            {
                ShareEntityDate sed = null;
                if (sdv.Id.HasValue)
                    sed = _shareEntityDateRepository.Find(sdv.Id.Value);
                else
                {
                    sed = new ShareEntityDate
                    {
                        ShareEntity = se
                    };
                    _shareEntityDateRepository.Add(sed);
                }

                sed.FromDate = DateTime.Parse(sdv.FromStr);
                sed.ToDate = string.IsNullOrEmpty(sdv.ToStr) ? (DateTime?)null : DateTime.Parse(sdv.ToStr);
                sed.Shares = sdv.Shares;
            }

            _shareEntityRepository.SaveChanges();

            return Json("");
        }

        //load months

        //load balances
        public ActionResult LoadBalances()
        {
            var list = _trxRepository.Get.Where(x => x.Payment == null).ToList();
            List<BalanceVM> tmpBalances = new List<BalanceVM>();
            foreach (var item in list)
            {
                if (!tmpBalances.Any(x => x.FromId == item.FromEntity.Id && x.ToId == item.ToEntity.Id))
                {
                    tmpBalances.Add(new BalanceVM
                    {
                        FromId = item.FromEntity.Id,
                        From = item.FromEntity.Name,
                        ToId = item.ToEntity.Id,
                        To = item.ToEntity.Name,
                    });
                }
            }

            foreach (BalanceVM b in tmpBalances)
            {
                b.Amount = list.Where(x => x.FromEntity.Id == b.FromId && x.ToEntity.Id == b.ToId).Sum(x => x.Amount);
            }

            //cancel out if needed
            List<BalanceVM> balances = new List<BalanceVM>();
            foreach (BalanceVM b in tmpBalances)
            {
                BalanceVM bal = balances.FirstOrDefault(x => (x.FromId == b.FromId && x.ToId == b.ToId) 
                    || ((x.FromId == b.ToId && x.ToId == b.FromId)));

                //if there is no balance for this combination of entiies, add it and calculate
                if (bal==null)
                {
                    balances.Add(new BalanceVM
                    {
                        FromId = b.FromId,
                        From = b.From,
                        ToId = b.ToId,
                        To = b.To,
                        Amount = b.Amount
                    });
                }
                //adjust the balance between the two as needed
                else
                {
                    if (bal.From == b.From)
                        bal.Amount += b.Amount;
                    else
                        bal.Amount -= b.Amount;
                    if (bal.Amount < 0)
                    {
                        var tmp = bal.From;
                        bal.From = bal.To;
                        bal.To = tmp;
                        bal.Amount = bal.Amount*-1;
                    }
                    if (bal.Amount == 0)
                        balances.Remove(bal);
                }
            }

            return Json(balances, JsonRequestBehavior.AllowGet);
        }


        public ActionResult SaveTx(TxVM model)
        {
            switch (model.Type)
            {
                case "purchase":
                    savePurchase(model);
                    break;
                case "owe":
                    saveOwe(model);
                    break;
                case "paid":
                    savePaid(model);
                    break;
            }

            DateTime dte = DateTime.Parse(model.PaidDate);

            return Json(new{number=dte.Month,year=dte.Year}, JsonRequestBehavior.AllowGet);
        }


        private void savePurchase(TxVM model)
        {
            //enter purchase record
            Purchase p = new Purchase
            {
                ShareEntity = _shareEntityRepository.Find(model.EntityFrom),
                PurchaseDate = DateTime.Parse(model.PaidDate),
                Split = model.PType=="split",
                Category = model.Category,
                Note = model.Desc,
                House = _houseRepository.Find(_HOUSEID),
                FromDate = model.PType != "split" ? (DateTime?)null : DateTime.Parse(model.SplitFrom),
                ToDate = model.PType != "split" ? (DateTime?)null : DateTime.Parse(model.SplitTo),
                Amount = model.Amount
            };
            _purchaseRepository.Add(p);

            //if 'split' enter in a money transaction for each entity on each day in the range specified
            if (p.Split)
            {
                DateTime activeDate = p.FromDate.Value;
                decimal totalDays = (decimal)(p.ToDate.Value - p.FromDate.Value).TotalDays+1;
                decimal amtPerDay = p.Amount/totalDays;
                //find date range for the split
                //on each day find the number of active shares
                //create a transaction for each day for each entity
                while (activeDate <= p.ToDate)
                {
                    var activeList = _shareEntityDateRepository.Get.Where(x => x.FromDate <= activeDate && (x.ToDate == null || x.ToDate >= activeDate));
                    decimal totalShares = activeList.Sum(x => x.Shares);
                    decimal perShare = amtPerDay / totalShares;
                    foreach (var entity in activeList.Where(x => x.ShareEntity.Id != p.ShareEntity.Id))
                    {
                        _trxRepository.Add(new MoneyTransaction
                        {
                            FromEntity = entity.ShareEntity,
                            ToEntity = p.ShareEntity,
                            Purchase = p,
                            TxDate = activeDate,
                            Amount = perShare * entity.Shares,
                            Note = p.Note,
                            House = p.House
                        });
                    }

                    activeDate = activeDate.AddDays(1);
                }

            }
            else //if 'single' enter in a money transaction for each entity
            {
                //find total active shares for entities active during the paid date
                var activeList = _shareEntityDateRepository.Get.Where(x => x.FromDate <= p.PurchaseDate && (x.ToDate == null || x.ToDate >= p.PurchaseDate)).ToList();

                foreach (var a in model.Shares)
                {
                    ShareEntityDate sed = activeList.Single(x => x.ShareEntity.Id == a.Id);
                    if (sed.Shares != a.Shares)
                    {
                        p.PurchaseShares.Add(new PurchaseShare
                        {
                            ShareEntity = sed.ShareEntity,
                            Shares = a.Shares
                        });
                    }
                }

                decimal totalShares = model.Shares.Sum(x => x.Shares);
                decimal perShare = p.Amount/totalShares;
                foreach (var entity in activeList.Where(x => x.ShareEntity.Id != p.ShareEntity.Id))
                {
                    EntityShareVM esv = model.Shares.Single(x => x.Id == entity.ShareEntity.Id);
                    if (esv.Shares != 0)
                    {
                        _trxRepository.Add(new MoneyTransaction
                        {
                            FromEntity = entity.ShareEntity,
                            ToEntity = p.ShareEntity,
                            Purchase = p,
                            TxDate = p.PurchaseDate,
                            Amount = perShare * esv.Shares,
                            Note = p.Note,
                            House = p.House
                        });
                    }
                }
            }
            _purchaseRepository.SaveChanges();
        }
        private void saveOwe(TxVM model)
        {
            //add in one owe record
            Owe o = new Owe
            {
                FromEntity = _shareEntityRepository.Find(model.EntityFrom),
                ToEntity = _shareEntityRepository.Find(model.EntityTo),
                OweDate = DateTime.Parse(model.PaidDate),
                Amount = model.Amount,
                Note = model.Desc,
                House = _houseRepository.Find(_HOUSEID)
            };
            _oweRepository.Add(o);

            //add in one money tranasaction record
            _trxRepository.Add(new MoneyTransaction
            {
                Owe = o,
                TxDate = o.OweDate,
                Amount = o.Amount,
                Note = o.Note,
                House = o.House,
                FromEntity = o.FromEntity,
                ToEntity = o.ToEntity
            });

            _oweRepository.SaveChanges();

            CalculatePaid(o.ToEntity.Id, o.FromEntity.Id, o.Amount, null, o);
        }
        private void savePaid(TxVM model)
        {
            //add in one payment record //add in one owe record
            Payment p = new Payment
            {
                FromEntity = _shareEntityRepository.Find(model.EntityFrom),
                ToEntity = _shareEntityRepository.Find(model.EntityTo),
                PaymentDate = DateTime.Parse(model.PaidDate),
                Amount = model.Amount,
                Note = model.Desc,
                House = _houseRepository.Find(_HOUSEID)
            };
            _paymentRepository.Add(p);

            CalculatePaid(p.FromEntity.Id, p.ToEntity.Id, p.Amount,p,null);
        }


        //if x paid y, then x PAY y, 
        //if x owe y,  then y PAY x
        private void CalculatePaid(int fromEntityId, int toEntityId, decimal amount, Payment p, Owe po)
        {
            var list = _trxRepository.Get.Where(x => x.FromEntity.Id == fromEntityId
                                                     && x.ToEntity.Id == toEntityId && x.Payment == null)
                                                     .OrderBy(x => x.TxDate).ThenBy(x => x.Id)
                                                     .ToList();

            decimal amtRemaining = amount;
            //go through money transactions and 'pay off' as needed
            //split money transaction if needed
            foreach (MoneyTransaction tx in list)
            {
                if (amtRemaining > 0)
                {
                    //if the amount being paid back is more than the amount in a transaction, pay off entire tx
                    if (amtRemaining >= tx.Amount)
                    {
                        if (p != null)
                            tx.Payment = p;
                        else
                            tx.PaymentOwe = po;
                        amtRemaining -= tx.Amount;
                    }
                    else
                    {
                        //pay of what can be paid, create a second tranaction
                        MoneyTransaction tx2 = new MoneyTransaction
                        {
                            Owe = tx.Owe,
                            Purchase = tx.Purchase,
                            TxDate = tx.TxDate,
                            Amount = tx.Amount - amtRemaining,
                            Note = "[Split] " + tx.Note,
                            House = tx.House,
                            FromEntity = tx.FromEntity,
                            ToEntity = tx.ToEntity
                        };

                        //pay of what can be paid
                        tx.Amount = amtRemaining;
                        if (p != null)
                            tx.Payment = p;
                        else
                            tx.PaymentOwe = po;
                        _trxRepository.Add(tx2);
                        amtRemaining = 0;
                    }
                }
            }
            _paymentRepository.SaveChanges();

            //if anything is left over, add in an 'owe' going back to the person that overpaid
            if (amtRemaining > 0 && p != null)
            {
                //add in one owe record
                Owe o = new Owe
                {
                    FromEntity = p!=null?p.ToEntity:po.FromEntity,
                    ToEntity = p!=null?p.FromEntity:po.ToEntity,
                    OweDate = p!=null?p.PaymentDate:po.OweDate,
                    Amount = amtRemaining,
                    Note = "Overpayment of $" + amtRemaining.ToString("n2"),
                    House = _houseRepository.Find(_HOUSEID)
                };
                _oweRepository.Add(o);

                //add in one money tranasaction record
                _trxRepository.Add(new MoneyTransaction
                {
                    Owe = o,
                    TxDate = o.OweDate,
                    Amount = o.Amount,
                    Note = o.Note,
                    House = o.House,
                    FromEntity = o.FromEntity,
                    ToEntity = o.ToEntity
                });

                _oweRepository.SaveChanges();
            }
        }

        public ActionResult LoadMonths()
        {
            //get all months for the house that have data
            var pays = from p in _paymentRepository.Get
                            group p by new { month = p.PaymentDate.Month, year = p.PaymentDate.Year } into d
                            select new {
                                m = d.Key.month,
                                y = d.Key.year};
            var owes = from p in _oweRepository.Get
                        group p by new { month = p.OweDate.Month, year = p.OweDate.Year } into d
                        select new {
                                m = d.Key.month,
                                y = d.Key.year};
            var purchases = from p in _purchaseRepository.Get
                        group p by new { month = p.PurchaseDate.Month, year = p.PurchaseDate.Year } into d
                        select new { 
                                m = d.Key.month,
                                y = d.Key.year};

            var months = pays.Union(owes).Union(purchases).Distinct().OrderByDescending(x => x.y).ThenByDescending(x => x.m).Select(x => new MonthVM
            {
                Year = x.y,
                Number = x.m,
                Name = x.m + " / " + x.y,
                Loaded = false
            });

            return Json(months,JsonRequestBehavior.AllowGet);
        }

        public ActionResult LoadMonth(int m, int yr)
        {
            var month = new MonthVM
            {
                Year = yr,
                Number = m,
                Name = m + " / " + yr,
                Food = 0,
                Utilities = 0,
                Misc = 0,
                Loaded= true,
                Transactions = new List<MonthTxVM>()
            };
            foreach (Owe tx in _oweRepository.Get.Where(x => x.OweDate.Month == m && x.OweDate.Year == yr).OrderBy(x => x.OweDate))
            {
                month.Transactions.Add(new MonthTxVM
                {
                    Type = "Owe",
                    Date = tx.OweDate.ToString("MM/dd/yyyy"),
                    Amount = tx.Amount,
                    Text = tx.FromEntity.Name + " owes " + tx.ToEntity.Name,
                    Note = tx.Note
                });
            }

            foreach (Purchase tx in _purchaseRepository.Get.Where(x => x.PurchaseDate.Month == m && x.PurchaseDate.Year == yr).OrderBy(x => x.PurchaseDate))
            {

                if (tx.Category == "food")
                    month.Food += tx.Amount;
                else if (tx.Category == "utility")
                    month.Utilities += tx.Amount;
                else
                    month.Misc += tx.Amount;
                month.Transactions.Add(new MonthTxVM
                {
                    Type = "Purchase",
                    Date = tx.PurchaseDate.ToString("MM/dd/yyyy"),
                    Amount = tx.Amount,
                    Text = tx.ShareEntity.Name + " made a purchase",
                    Note = tx.Note
                });
            }
            foreach (Payment tx in _paymentRepository.Get.Where(x => x.PaymentDate.Month == m && x.PaymentDate.Year == yr).OrderBy(x => x.PaymentDate))
            {
                
                month.Transactions.Add(new MonthTxVM
                {
                    Type = "Payment",
                    Date = tx.PaymentDate.ToString("MM/dd/yyyy"),
                    Amount = tx.Amount,
                    Text = tx.FromEntity.Name + " paid " + tx.ToEntity.Name,
                    Note = tx.Note
                });
            }

            return Json(month, JsonRequestBehavior.AllowGet);
        }
    }
}
