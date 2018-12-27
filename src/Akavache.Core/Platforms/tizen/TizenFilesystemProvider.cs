using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using Akavache;
using Tizen.Applications;

namespace Akavache
{
	public class TizenFilesystemProvider : IFilesystemProvider
	{
		readonly SimpleFilesystemProvider _inner = new SimpleFilesystemProvider();

		public IObservable<Stream> OpenFileForReadAsync(string path, IScheduler scheduler)
		{
			return _inner.OpenFileForReadAsync(path, scheduler);
		}

		public IObservable<Stream> OpenFileForWriteAsync(string path, IScheduler scheduler)
		{
			return _inner.OpenFileForWriteAsync(path, scheduler);
		}

		public IObservable<Unit> CreateRecursive(string path)
		{
			return _inner.CreateRecursive(path);
		}

		public IObservable<Unit> Delete(string path)
		{
			return _inner.Delete(path);
		}

		public string GetDefaultLocalMachineCacheDirectory()
		{
			return Application.Current.DirectoryInfo.Cache;
		}

		public string GetDefaultRoamingCacheDirectory()
		{
			return Application.Current.DirectoryInfo.ExternalCache;
		}

		public string GetDefaultSecretCacheDirectory()
		{
			var path = Application.Current.DirectoryInfo.ExternalCache;
			var di = new System.IO.DirectoryInfo(Path.Combine(path, "Secret"));
			if (!di.Exists) di.CreateRecursive();

			return di.FullName;
		}
	}
}

