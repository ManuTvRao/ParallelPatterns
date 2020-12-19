using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Aggregate
{
    public static class ParallelAggregate
    {
        private static object _finalResultLock = new object();
        public static TResult Aggregate<TSource, TAccumulate, TResult>(
            this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> intermediateReduceFunc,
        Func<TAccumulate, TAccumulate, TAccumulate> finalReduceFunc,
        Func<TAccumulate, TResult> resultSelector)
        {
            TResult finalResult = default(TResult);
            Int32 noOfProcessors = Environment.ProcessorCount;
            TAccumulate intermittentResult = default(TAccumulate);
            using (CountdownEvent countdownEvent = new CountdownEvent(noOfProcessors + 1))
            {
                countdownEvent.AddCount(1);
                foreach (var partion in Partitioner.Create(source).GetPartitions(noOfProcessors))
                {
                    ThreadPool.QueueUserWorkItem((initialSeed) =>
                    {
                        TAccumulate accumulate = (TAccumulate)initialSeed;
                        while (partion.MoveNext())
                        {
                            accumulate = intermediateReduceFunc(accumulate, partion.Current);
                        }
                        bool lockTaken = false;
                        try
                        {
                            Monitor.TryEnter(_finalResultLock, ref lockTaken);
                            if (lockTaken)
                            {
                                intermittentResult = finalReduceFunc(intermittentResult, accumulate);
                            }
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            if (lockTaken)
                                Monitor.Exit(_finalResultLock);
                            countdownEvent.Signal();
                        }

                    }, seed);
                }
                countdownEvent.Signal();
                countdownEvent.Wait();
                finalResult = resultSelector(intermittentResult);
            }
            return finalResult;
        }
    }
}
