using ArchieverApp.Utils;
using System;
using System.IO;
using System.Threading;

namespace ArchieverApp.GZip
{
    public abstract class BaseGzip
    {
        #region Members

        protected const int _blockSize = 10485760; //10 MB

        protected FileStream _fsReader;

        protected FileStream _fsWriter;

        protected string _sourceFile;
        protected string _destinationFile;

        protected bool _isCancelled;
        protected bool _isSucceded;

        protected static int _threads = Environment.ProcessorCount;

        protected int _finishedThreads = 0;

        protected ManualResetEvent[] _doneEvents = new ManualResetEvent[_threads];

        protected QueueManager _queueWriter = new QueueManager();
        protected QueueManager _queueReader = new QueueManager();

        protected static Mutex _mutexReader = new Mutex();

        protected static Mutex _mutexWriter = new Mutex();

        protected BlockManager _blockManager = new BlockManager();

        #endregion

        #region Properties

        public bool IsSucceeded => _isSucceded;

        public Exception ThreadException { get; set; }

        #endregion

        #region Constructors

        public BaseGzip(string sourceFile, string destinationFile)
        {
            _sourceFile = sourceFile;
            _destinationFile = destinationFile;
            _fsReader = new FileStream(_sourceFile, FileMode.Open);            
        }

        #endregion

        #region Methods

        public void Launch()
        {
            var _reader = new Thread(new ThreadStart(Read));
            _reader.Start();

            for (var i = 0; i < _threads; i++)
            {
                _doneEvents[i] = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(Compress, i);
            }

            var _writer = new Thread(new ThreadStart(Write));
            _writer.Start();

            _writer.Join();

            _isSucceded = !_isCancelled;
        }

        protected abstract void Read();

        protected abstract void Write();

        protected abstract void Compress(object i);

        public int CallBackResult() => !_isCancelled && _isSucceded ? 0 : 1;

        public void Cancel()
        {
            _isCancelled = true;
            foreach (var doneEvent in _doneEvents)
            {
                doneEvent.Set();
            }
            _fsReader.Close();
            _fsWriter.Close();
        }

        protected virtual void SetDone(int i)
        {
            _doneEvents[i].Set();
            _finishedThreads++;
            if (_finishedThreads == _threads)
            {
                _queueWriter.Stop();
            }
        }

        public void ThrowException(Exception ex)
        {
            ThreadException = ex;
            Cancel();
        }

        #endregion
    }
}
