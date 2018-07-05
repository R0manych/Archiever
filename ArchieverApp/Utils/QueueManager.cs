using ArchieverApp.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ArchieverApp.Utils
{
    public class QueueManager
    {
        #region Members

        private Queue<ByteBlock> _queue = new Queue<ByteBlock>();
        private bool _isStopped = false;
        private int _blockId = 0;

        #endregion

        #region Methods

        public void Enqueue(ByteBlock block)
        {
            lock (_queue)
            {
                if (_isStopped)
                    throw new InvalidOperationException("Queue already stopped");

                while (block.ID != _blockId)
                {
                    Monitor.Wait(_queue);
                }
                _queue.Enqueue(block);
                _blockId++;
                Monitor.PulseAll(_queue);
            }
        }  
        
        public ByteBlock Dequeue()
        {
            lock (_queue)
            {
                if (_queue.Count == 0)
                    return null;

                return _queue.Dequeue();
            }
        }

        public void Stop()
        {
            lock (_queue)
            {
                _isStopped = true;
                Monitor.PulseAll(_queue);
            }
        }

        #endregion
    }
}
