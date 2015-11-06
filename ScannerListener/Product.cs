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
        private string ean;

        public Product(string number, string qty, string name, string location, string ean)
        {
            this.number = number;
            this.qty = qty;
            this.name = name;
            this.location = location;
            this.ean = ean;
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

        public string getEAN()
        {
            return ean;
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

        public void setEAN(string ean)
        {
            if (ean.Length > 0)
            {
                this.ean = ean;
            }
        }
    }
}
