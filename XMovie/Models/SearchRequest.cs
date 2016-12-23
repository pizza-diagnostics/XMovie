using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XMovie.Models
{
    public class SearchRequest
    {
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private Queue<string> requestQueue = new Queue<string>();

        public string CreateSearchEntry()
        {
            return Guid.NewGuid().ToString();
        }

        public Task Entry(string entry)
        {
            System.Diagnostics.Debug.Print($"entry: {entry}");
            lock (requestQueue)
            {
                requestQueue.Enqueue(entry);
            }
            // unsafe?

            System.Diagnostics.Debug.Print($"wait : {entry}");
            return semaphore.WaitAsync();
        }

        public void Release(string entry)
        {
            System.Diagnostics.Debug.Print($"release : {entry}");
            lock (requestQueue)
            {
                var e = requestQueue.Dequeue();
                // e == entry
            }
            semaphore.Release();
        }

        public bool IsCancelled(string entry)
        {
            lock (requestQueue)
            {
                return requestQueue.Count() > 1;
            }
        }
    }
}
