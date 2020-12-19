using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleMapReduce
{
    public class Order
    {
        public Order(DateTime transactionDate, long quantity, double price,long quantityRemaining,string description,string code) 
        {
            TransctionDate = transactionDate;
            Quantity = quantity;
            Price = price;
            QuantityInStock = quantityRemaining;
            Description = description;
            Code = code;
        }
        public DateTime TransctionDate { get;}
        public long Quantity { get; }
        public double Price { get;}
        public double Value { get; }
        public long QuantityInStock { get; }
        public string Description { get; }
        public string Code { get; }

        public override string ToString()
        {
            return $"{TransctionDate.ToString()},{Quantity.ToString()},{Price.ToString()},{QuantityInStock.ToString()},{Description},{Code}"; 
        }
    }
}
