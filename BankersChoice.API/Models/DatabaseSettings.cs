using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankersChoice.API.Models
{
    public class DatabaseSettings
    {
        public string AccountsCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string DatabaseUserName { get; set; }
        public string DatabasePassword { get; set; }
    }
}
