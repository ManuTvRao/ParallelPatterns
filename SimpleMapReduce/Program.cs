using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleMapReduce
{
    class Program
    {
        private static object _orderLock = new object();
        private static object _ordersCSVLock = new object();
        static void Main(string[] args)
        {
            SaveOrdersForCities(GetOrdersForCities(new List<string>() { "Bangalore"/*,"Pune","Mumbai","New Delhi","Chennai"*/ }));
        }

        private static IEnumerable<Dictionary<string,List<Order>>> GetOrdersForCities(List<string> cities)
        {
            List<Dictionary<string, List<Order>>> ordersMap = new List<Dictionary<string, List<Order>>>();
            foreach (var city in cities)
            {
                ordersMap.Add(GetOrdersForCity(city));
            }
            return ordersMap;
        }

        private static Dictionary<string,List<Order>> GetOrdersForCity(string city)
        {
            bool lockTaken = false;
            Random random = new Random();
            List<Order> orders = new List<Order>();
            ParallelLoopResult loopResult = Parallel.For(1, 101,new ParallelOptions() { MaxDegreeOfParallelism = 1},
            () => new List<Order>(),
            (iterator,loopState,cityOrderList) => 
            {
                cityOrderList.Add(new Order(
                    DateTime.Now,
                    10,
                    100,
                    100000,
                    "Banana Republic",
                    "Banana-002"
                    ));
                    return cityOrderList;
            },
            (cityOrderList) => 
            {
                try
                {
                    Monitor.TryEnter(_orderLock, ref lockTaken);
                    orders.AddRange(cityOrderList);
                }
                catch (Exception)
                {
                }
                finally 
                {
                    if (lockTaken)
                        Monitor.Exit(_orderLock);
                }
            });
            return new Dictionary<string, List<Order>>() 
            {
                { city,orders}
            };
        }

        private static void SaveOrdersForCities(IEnumerable<Dictionary<string, List<Order>>> citiesOrdersMap)
        {
            foreach (var cityAndOrdersMap in citiesOrdersMap)
            {
                foreach (var cityOrder in cityAndOrdersMap)
                {
                    string pathToSave = @"C:\Users\Manu\source\repos\ParallelPatterns\SimpleMapReduce\OrdersRepo";
                    SaveOrdersForCity(pathToSave+cityOrder.Key+".csv",GenerateCsvFormat(cityOrder.Value));
                }
            }
        }
        private static void SaveOrdersForCity(string fileName,string csvOrders)
        {
            File.CreateText(fileName).Write(csvOrders);
        }
        private static string GenerateCsvFormat(List<Order> orders)
        {
            string completeOrder = string.Empty;
            bool lockTaken = false;
            return orders.AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .WithMergeOptions(ParallelMergeOptions.Default)
                .WithCancellation(CancellationToken.None)
                .WithDegreeOfParallelism(1)
                .Select(order => order.ToString()).Aggregate
                (
                    () => string.Empty,
                    (accumulator,orderCSV) => accumulator + orderCSV + Environment.NewLine,
                    (accumulator1, accumulator2) =>
                    {
                        try
                        {
                            Monitor.Enter(_ordersCSVLock, ref lockTaken);
                            if (lockTaken)
                                completeOrder = accumulator2 + accumulator1;
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            if (lockTaken)
                                Monitor.Exit(_ordersCSVLock);
                        }
                        return completeOrder;
                    },
                    (accumulator) => { return completeOrder; }
                );
        }
    }
}
