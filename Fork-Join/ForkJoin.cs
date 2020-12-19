using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fork_Join
{
    public static class ForkJoin
    {
        public static void ParallelFor<T>(int fromInclusive,int toExclusive, Action<T> action,T state) 
        {
            int numberOfThreads = Environment.ProcessorCount;
            int nextIteration = fromInclusive;
            using (CountdownEvent ce = new CountdownEvent(numberOfThreads))
            {
                for (int iteration = 0; iteration < numberOfThreads; iteration++)
                {
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        int index = 0;
                        while ((index = Interlocked.Increment(ref nextIteration) - 1) < toExclusive)
                        {
                            action(state);
                        }
                        ce.Signal();
                    });
                }
            }
        }

        public static void MyParallelFor<T>(int fromInclusive, int toExclusive, Action<T> action, T state)
        {
            int numberOfThreads = Environment.ProcessorCount;
            using (CountdownEvent ce = new CountdownEvent(numberOfThreads))
            {
                for (int iteration = 0; iteration < numberOfThreads; iteration++)
                {
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        while ((Interlocked.Increment(ref fromInclusive)) < toExclusive)
                        {
                            action(state);
                        }
                        ce.Signal();
                    });
                }
            }
        }

        public static void MyParallelForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            using (CountdownEvent ce = new CountdownEvent(1))
            {
                foreach (var element in sequence)
                {
                    T state = element;
                    ce.AddCount(1);
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        action(state);
                        ce.Signal();
                    });
                }
                ce.Signal();
                ce.Wait();
            }
        }

        public static void ParallelForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            using (CountdownEvent ce = new CountdownEvent(1))
            {
                foreach (var element in sequence)
                {
                    ce.AddCount(1);
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        action(element);
                        ce.Signal();
                    }, element, false);
                }
                ce.Signal();
                ce.Wait();
            }
        }

        public static void ParallelInvoke(params Action[] actions)
        {
            using (CountdownEvent ce = new CountdownEvent(1))
            {
                foreach (var action in actions)
                {
                    Action actionToExecute = action;
                    ce.AddCount(1);
                    ThreadPool.QueueUserWorkItem((dummy) =>
                    {
                        actionToExecute();
                        ce.Signal();
                    });
                }
                ce.Signal();
                ce.Wait();
            }
        }

        public static void ParallelInvoke<T>(T state,params Action<T>[] actions)
        {
            using (CountdownEvent ce = new CountdownEvent(1))
            {
                foreach (var action in actions)
                {
                    Action<T> actionToExecute = action;
                    ce.AddCount(1);
                    ThreadPool.QueueUserWorkItem((stateUsed) =>
                    {
                        actionToExecute(stateUsed);
                        ce.Signal();
                    },state,false);
                }
                ce.Signal();
                ce.Wait();
            }
        }

        public static void ParallelInvoke<T>(T[] states, params Action<T>[] actions)
        {
            using (CountdownEvent ce = new CountdownEvent(1))
            {
                int index = -1;
                foreach (var action in actions)
                {
                    T state = states[index++];
                    Action<T> actionToExecute = action;
                    ce.AddCount(1);
                    ThreadPool.QueueUserWorkItem((stateUsed) =>
                    {
                        actionToExecute(stateUsed);
                        ce.Signal();
                    }, state, false);
                }
                ce.Signal();
                ce.Wait();
            }
        }

        public static void ParallelInvokeFor<T>(T[] states, params Action<T>[] actions)
        {
            int totalCallCount = actions.Length;
            int numberOfCores = Environment.ProcessorCount;
            int initialCount = 0;

            using (CountdownEvent ce = new CountdownEvent(numberOfCores))
            {
                for (int iteration = 0; iteration < totalCallCount; iteration++)
                {
                    ThreadPool.QueueUserWorkItem((state) => 
                    {
                        int startIndex = 0;
                        while ((startIndex = Interlocked.Increment(ref initialCount) -1) < totalCallCount)
                        {
                            actions[startIndex](states[startIndex]);
                        }
                        ce.Signal();
                    });
                    ce.Wait();
                }
            }
        }

        public static void TaskBasedParallelInvoke<T>(params Action[] actions)
        {
            List<Task> taskList = new List<Task>();
            foreach (var action in actions)
            {
                taskList.Add(Task.Factory.StartNew(() => action()));
            }
            Task.WaitAll(taskList.ToArray());
        }

        public static void TaskBasedParallelInvoke1<T>(Action[] actions)
        {
            Task[] tasks = new Task[actions.Length];
            for (int iteration = 0; iteration < actions.Length; iteration++)
            {
                tasks[iteration] = Task.Factory.StartNew(() => actions[iteration]()); ;
            }
            Task.WaitAll(tasks);
        }

        public static T[] MyTaskBasedParallelInvoke<T>(Func<T>[] functions)
        {
            return functions.Select(function => Task.Factory.StartNew(() => function()).Result).ToArray();
        }

        public static T[] TaskBasedParallelInvoke<T>(Func<T>[] functions)
        {
            Task<T>[] tasks = functions.Select((function) =>
            {
                return Task.Factory.StartNew(function);
            }).ToArray();
            Task.WaitAll(tasks);
            return tasks.Select((task) => task.Result).ToArray();
        }

        public static T[] TaskBasedParallelInvoke1<T>(Func<T>[] functions)
        {
            T[] results = new T[functions.Length];
            Parallel.For(0, functions.Length, iteration =>
            {
                results[iteration] = functions[iteration]();
            });
            return results;
        }

        public static T[] TaskBasedParallelInvoke2<T>(Func<T>[] functions)
        {
            return functions.AsParallel().Select((func) => func()).ToArray();
        }

        public static T[] TaskBasedParallelInvoke3<T>(Func<T>[] functions)
        {
            T[] results = new T[functions.Length];
            Parallel.For(0, functions.Length, new ParallelOptions() { MaxDegreeOfParallelism = 2 },
            (iteration) =>
            {
                results[iteration] = functions[iteration]();
            });
            return results;
        }

        public static async Task<IEnumerable<T>> TaskBasedParallelInvoke4<T>(Func<T>[] functions)
        {
            Task<T>[] tasks = new Task<T>[functions.Length];
            for (int i = 0; i < functions.Length; i++)
            {
                Task<T> task = Task.Factory.StartNew<T>(() => functions[i]());
                tasks[i] = task;
            }
            var results = await Task.WhenAll<T>(tasks);
            return results;
        }

        public static async IAsyncEnumerable<T> TaskBasedParallelInvoke5<T>(Func<T>[] functions)
        {
            foreach (var func in functions)
            {
                yield return await Task.Factory.StartNew(() => func());
            }
        }

        public static async Task<IEnumerable<T>> TaskBasedParallelInvoke6<T>(Func<T>[] functions)
        {
            T[] results = new T[functions.Length];
            for (int i = 0; i < functions.Length; i++)
            {
                results[i] = await Task.Factory.StartNew<T>(() => functions[i]());
            }
            return results;
        }

        public static IEnumerable<Task<T>> TaskBasedParallelInvoke7<T>(Func<T>[] functions)
        {
            Task<T>[] tasks = new Task<T>[functions.Length];
            for (int i = 0; i < functions.Length; i++)
            {
                Task<T> task = Task.Factory.StartNew<T>(() => functions[i]());
                tasks[i] = task;
            }
            return tasks;
        }
    }
}
