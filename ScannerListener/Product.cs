using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScannerListener
{
    class Product
    {
        private int number;
        private int qty;
        private string name;
        private string location;

        public Product(int number, int qty, string name, string location)
        {
            this.number = number;
            this.qty = qty;
            this.name = name;
            this.location = location;
        }

        public int getNumber()
        {
            return number;
        }

        public int getQty()
        {
            return qty;
        }

        public string getName()
        {
            return name;
        }

        public string getLocation()
        {
            return location;
        }
    }
}
