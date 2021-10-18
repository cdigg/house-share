using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HouseShare.Domain.Repositories.Abstract;

namespace HouseShare.Domain.Repositories.Concrete
{
    public class DbMoneyTransaction : DbRepository<MoneyTransaction, HouseShareEntities>, IMoneyTransaction
    {
        public DbMoneyTransaction(HouseShareEntities ptContext)
            : base(ptContext)
        { }

        private HouseShareEntities context
        {
            get { return Context as HouseShareEntities; }
        }
    }
}
