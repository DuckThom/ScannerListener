using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScannerListener
{
    class Product
    {
        private string number;
        private string qty;
        private string name;
        private string location;

        public Product(string number, string qty, string name, string location)
        {
            this.number = number;
            this.qty = qty;
            this.name = name;
            this.location = location;
        }

        public string getNumber()
        {
            return number;
        }

        public string getQty()
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

        public void setName(string name)
        {
            if (name.Length > 0)
            {
                this.name = name;
            }
        }

        public void setLocation(string location)
        {
            if (location.Length > 0)
            {
                this.location = location;
            }
        }
    }
}
