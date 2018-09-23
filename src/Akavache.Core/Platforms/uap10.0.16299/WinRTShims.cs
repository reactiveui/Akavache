using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace System.IO
{
    public static class MicrosoftStreamExtensions
    {
        public static IRandomAccessStream AsRandomAccessStream(this Stream stream)
        {
            return new RandomStream(stream);
        }

        public static IRandomAccessStream AsRandomAccessStream(this byte[] bytes)
        {
            return new RandomStream(bytes);
        }
    }

    public class RandomStream : IRandomAccessStream
    {
        private Stream streamValue;

        public RandomStream(Stream stream)
        {
            streamValue = stream;
        }

        public RandomStream(byte[] bytes)
        {
            streamValue = new MemoryStream(bytes);
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            if ((long)position > streamValue.Length) {
                throw new IndexOutOfRangeException();
            }

            streamValue.Position = (long)position;

            return streamValue.AsInputStream();
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            if ((long)position > streamValue.Length) {
                throw new IndexOutOfRangeException();
            }

            streamValue.Position = (long)position;

            return streamValue.AsOutputStream();
        }

        public ulong Size
        {
            get
            {
                return (ulong)streamValue.Length;
            }
            set
            {
                streamValue.SetLength((long)value);
            }
        }

        public bool CanRead
        {
            get
            {
                return true;
            }
        }

        public bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public IRandomAccessStream CloneStream()
        {
            throw new NotSupportedException();
        }

        public ulong Position
        {
            get
            {
                return (ulong)streamValue.Position;
            }
        }

        public void Seek(ulong position)
        {
            streamValue.Seek((long)position, 0);
        }

        public void Dispose()
        {
            streamValue.Dispose();
        }

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            throw new NotSupportedException();
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            throw new NotImplementedException();
        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}

namespace System.IO.IsolatedStorage
{
    internal class IsolatedStorageException : Exception { }
}
