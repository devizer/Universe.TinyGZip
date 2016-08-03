## Universe.TinyGZip [![Build Status](https://travis-ci.org/devizer/Universe.TinyGZip.svg?branch=master)](https://travis-ci.org/devizer/Universe.TinyGZip)
The build artefact is the single source file: 
https://github.com/devizer/Universe.TinyGZip/blob/master/out/Universe.TinyGZip.cs

The namespace contains 5 types:
- `GZipStream`
- `ParallelDeflateOutputStream`
- `CompressionLevel`
- `CompressionMode`
- `ZlibException`

## perfomance
The worst case on Windows .NET
- Tiny compression is slower than System compression by 40%
- Tiny decompression is slower then System by 7%

The worst case on Linux with native gzip library under System GZipStream
- Tiny compression is slower than System compression by 2,5 times
- Tiny decompression is slower than System one by 3,6 times

The best case on Linux with native gzip library under System GZipStream
- Tiny compression is slower than System compression by 2,2 times
- Tiny decompression is slower than System one by 3,1 times

The best case is compression of random data. The worst - lorem ipsum

## may be useful?
Method checks is System.IO.Compression.GZipStream supported and returns either System stream of Tiny stream depending on check result:
```csharp
public static Stream GZipExtentions.CreateDecompressor(Stream gzipped)
```

