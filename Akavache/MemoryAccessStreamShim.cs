using System;
using System.IO;
using Windows.Storage.Streams;

namespace Akavache
{
    internal class MemoryRandomAccessStream : IRandomAccessStream
    {
        readonly Stream internalStream;

        public MemoryRandomAccessStream(Stream stream)
        {
            this.internalStream = stream;
        }

        public MemoryRandomAccessStream(byte[] bytes)
        {
            this.internalStream = new MemoryStream(bytes);
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            this.internalStream.Seek((long)position, SeekOrigin.Begin);

            return this.internalStream.AsInputStream();
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            this.internalStream.Seek((long)position, SeekOrigin.Begin);

            return this.internalStream.AsOutputStream();
        }

        public ulong Size
        {
            get { return (ulong)this.internalStream.Length; }
            set { this.internalStream.SetLength((long)value); }
        }

        public bool CanRead
        {
            get { return true; }
        }

        public bool CanWrite
        {
            get { return true; }
        }

        public IRandomAccessStream CloneStream()
        {
            throw new NotSupportedException();
        }

        public ulong Position
        {
            get { return (ulong)this.internalStream.Position; }
        }

        public void Seek(ulong position)
        {
            this.internalStream.Seek((long)position, 0);
        }

        public void Dispose()
        {
            this.internalStream.Dispose();
        }

        public Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            var inputStream = this.GetInputStreamAt(0);
            return inputStream.ReadAsync(buffer, count, options);
        }

        public Windows.Foundation.IAsyncOperation<bool> FlushAsync()
        {
            var outputStream = this.GetOutputStreamAt(0);
            return outputStream.FlushAsync();
        }

        public Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            var outputStream = this.GetOutputStreamAt(0);
            return outputStream.WriteAsync(buffer);
        }
    }


}
