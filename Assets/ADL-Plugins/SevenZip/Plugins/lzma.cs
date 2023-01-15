using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

public class lzma {
#if !UNITY_WEBPLAYER  || UNITY_EDITOR
	//if you want to be able to call the functions: get7zinfo, get7zSize, decode2Buffer from a thread set this string before to the Application.persistentDataPath !
	public static string persitentDataPath="";

    internal static int[] props = new int [7];
    internal static bool defaultsSet = false;

    public  enum dic : int {
        K0004 = 4096,
        K0008 = 8192,
        K0016 = 16384,
        K0032 = 32768,
        K0064 = 65536,
        K0128 = 131072,
        K0256 = 262144,
        K0512 = 524288,
        K1024 = 1048576,
        K2048 = 2097152
    }


    /*
     level
        Description: The compression level.
        Range: [0;9].
        Default: 5.

    dictSize
        Description: The dictionary size.
        Range: [1<<12;1<<27] for 32-bit version or [1<<12;1<<30] for 64-bit version.
        Default: 1<<24.

    lc
        Description: The number of high bits of the previous byte to use as a context for literal encoding.
        Range [0;8].
        Default: 3
        Sometimes lc = 4 gives gain for big files.

    lp
        Description: The number of low bits of the dictionary position to include in literal_pos_state.
        Range: [0;4].
        Default: 0.
        It is intended for periodical data when period is equal 2^value (where lp=value). For example, for 32-bit (4 bytes) periodical data you can use lp=2. Often it's better to set lc=0, if you change lp switch.

    pb
        Description: pb is the number of low bits of the dictionary position to include in pos_state.
        Range: [0;4].
        Default: 2.
        It is intended for periodical data when period is equal 2^value (where lp=value).

    fb
        Description: Sets the number of fast bytes for the Deflate/Deflate64 encoder.
        Range: [5;255].
        Default: 128.
        Usually, a big number gives a little bit better compression ratio and a slower compression process. A large fast bytes parameter can significantly increase the compression ratio for files which contain long identical sequences of bytes.

    numThreads
        Description: Number of threads.
        Options: 1 or 2
        Default: 2
    */

    //0 = level, /* 0 <= level <= 9, default = 5 */
	//1 = dictSize, /* use (1 << N) or (3 << N). 4 KB < dictSize <= 128 MB */4194304
	//2 = lc, /* 0 <= lc <= 8, default = 3  */
	//3 = lp, /* 0 <= lp <= 4, default = 0  */
	//4 = pb, /* 0 <= pb <= 4, default = 2  */
	//5 = fb,  /* 5 <= fb <= 273, default = 32 */
	//6 = numThreads /* 1 or 2, default = 2 */

	//A function that sets the compression properties for the lzma compressor. Will affect the lzma alone file and the lzma buffer compression.
	//A simple usage of this function is to call it only with the 1st parameter that sets the compression level: setProps(9);
	//
	//Multithread safe advice: call this function before starting any thread operations !!!
    public static void setProps(int level = 5, int dictSize = 65536, int lc = 3, int lp = 0, int pb = 2, int fb = 32, int numThreads = 2) {
        defaultsSet = true;
        props[0] = level;
        props[1] = dictSize;
        props[2] = lc;
        props[3] = lp;
        props[4] = pb;
        props[5] = fb;
        props[6] = numThreads;
    }


#if (UNITY_IOS || UNITY_TVOS || UNITY_IPHONE || UNITY_WEBGL) && !UNITY_EDITOR
#if (UNITY_IOS || UNITY_TVOS || UNITY_IPHONE) && !UNITY_WEBGL
        #if !UNITY_TVOS
        [DllImport("__Internal")]
        public static extern void sevenZcancel();
		[DllImport("__Internal")]
		public static extern int lsetPermissions(string filePath, string _user, string _group, string _other);
		[DllImport("__Internal")]
		private static extern int decompress7zip(string filePath, string exctractionPath, bool fullPaths,  string entry, IntPtr progress, IntPtr FileBuffer, int FileBufferLength);
		[DllImport("__Internal")]
		private static extern int decompress7zip2(string filePath, string exctractionPath, bool fullPaths, string entry, IntPtr progress, IntPtr FileBuffer, int FileBufferLength);
		[DllImport("__Internal")]
		internal static extern int lzmaUtil(bool encode, string inPath, string outPath, IntPtr Props);
        #endif
		[DllImport("__Internal")]
		private static extern IntPtr _getSize(string filePath, IntPtr FileBuffer, int FileBufferLength, bool justParse);
		[DllImport("__Internal")]
		private static extern  ulong entrySize(string filePath, string entry, IntPtr FileBuffer, int FileBufferLength);
		[DllImport("__Internal")]
		internal static extern int decode2Buf(string filePath, string entry,  IntPtr buffer, IntPtr FileBuffer, int FileBufferLength);
        [DllImport("__Internal")]
        public static extern void resetBytesRead();
        [DllImport("__Internal")]
        public static extern ulong getBytesRead();
        [DllImport("__Internal")]
        public static extern ulong getBytesWritten();
        [DllImport("__Internal")]
        public static extern IntPtr _createBuffer(int size);
        [DllImport("__Internal")]
        internal static extern void _addToBuffer(IntPtr destination, int offset, IntPtr buffer, int len);
#endif
#if (UNITY_IOS || UNITY_TVOS || UNITY_IPHONE || UNITY_WEBGL)
		[DllImport("__Internal")]
		public static extern void _releaseBuffer(IntPtr buffer);	
		[DllImport("__Internal")]
		internal static extern IntPtr Lzma_Compress( IntPtr buffer, int bufferLength, bool makeHeader, ref int v, IntPtr Props);
		[DllImport("__Internal")]
		internal static extern int Lzma_Uncompress( IntPtr buffer, int bufferLength, int uncompressedSize,  IntPtr outbuffer,bool useHeader);
#endif
#endif

#if UNITY_5_4_OR_NEWER
#if (UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_WEBGL) && !UNITY_EDITOR || UNITY_EDITOR_LINUX
		private const string libname = "lzma";
#elif UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
    private const string libname = "liblzma";
	#endif
#else
	#if (UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_WEBGL) && !UNITY_EDITOR 
		private const string libname = "lzma";
	#endif
	#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
		private const string libname = "liblzma";
	#endif
#endif

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_STANDALONE_LINUX
	#if (!UNITY_WEBGL || UNITY_EDITOR)
		#if (UNITY_STANDALONE_OSX  || UNITY_STANDALONE_LINUX || UNITY_ANDROID || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)&& !UNITY_EDITOR_WIN
			//set permissions of a file in user, group, other. Each string should contain any or all chars of "rwx".
			//returns 0 on success
			[DllImport(libname, EntryPoint = "lsetPermissions")]
			internal static extern int lsetPermissions(string filePath, string _user, string _group, string _other);
		#endif
        [DllImport(libname, EntryPoint = "decompress7zip"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]
        internal static extern int decompress7zip(string filePath, string exctractionPath, bool fullPaths,  string entry, IntPtr progress, IntPtr FileBuffer, int FileBufferLength);

		[DllImport(libname, EntryPoint = "decompress7zip2"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]
		internal static extern int decompress7zip2(string filePath, string exctractionPath, bool fullPaths, string entry, IntPtr progress, IntPtr FileBuffer, int FileBufferLength);

		[DllImport(libname, EntryPoint = "_getSize"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]

		internal static extern IntPtr _getSize(string filePath, IntPtr FileBuffer, int FileBufferLength, bool justParse);

		[DllImport(libname, EntryPoint = "entrySize"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]
		internal static extern ulong entrySize(string filePath, string entry, IntPtr FileBuffer, int FileBufferLength);

		[DllImport(libname, EntryPoint = "lzmaUtil"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]
		internal static extern int lzmaUtil(bool encode, string inPath, string outPath, IntPtr Props);

		[DllImport(libname, EntryPoint = "decode2Buf"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]
		internal static extern int decode2Buf(string filePath, string entry,  IntPtr buffer, IntPtr FileBuffer, int FileBufferLength);
	#endif
		[DllImport(libname, EntryPoint = "_releaseBuffer"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]
		public static extern void _releaseBuffer(IntPtr buffer);

        [DllImport(libname, EntryPoint = "_createBuffer"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		    , CallingConvention = CallingConvention.Cdecl
        #endif
        )]
        public static extern IntPtr _createBuffer(int size);

        [DllImport(libname, EntryPoint = "_addToBuffer"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		    , CallingConvention = CallingConvention.Cdecl
        #endif
        )]
        private static extern void _addToBuffer(IntPtr destination, int offset, IntPtr buffer, int len);

		[DllImport(libname, EntryPoint = "Lzma_Compress"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]
		internal static extern IntPtr Lzma_Compress( IntPtr buffer, int bufferLength, bool makeHeader, ref int v, IntPtr Props);

		[DllImport(libname, EntryPoint = "Lzma_Uncompress"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]
		internal static extern int Lzma_Uncompress( IntPtr buffer, int bufferLength, int uncompressedSize, IntPtr outbuffer,bool useHeader);

        // Send cancel signal when decompressing a 7z archive with multiple files
        [DllImport(libname, EntryPoint = "sevenZcancel"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]
        public static extern void sevenZcancel();

        // reset the global bytes written and read variables
        [DllImport(libname, EntryPoint = "resetBytesRead"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]
        public static extern void resetBytesRead();

        // returns the bytes read by the plugin (in real time if called from a thread).
        [DllImport(libname, EntryPoint = "getBytesRead"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]
        public static extern ulong getBytesRead();

        // returns the bytes written by the plugin (in real time if called from a thread).
        [DllImport(libname, EntryPoint = "getBytesWritten"
        #if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_ANDROID
		, CallingConvention = CallingConvention.Cdecl
        #endif
        )]
        public static extern ulong getBytesWritten();
#endif

    internal static GCHandle gcA(object o) {
        return GCHandle.Alloc(o, GCHandleType.Pinned);
    }

#if !UNITY_WEBGL || UNITY_EDITOR
	// set permissions of a file in user, group, other.
	// Each string should contain any or all chars of "rwx".
	// returns 0 on success
    #if !UNITY_TVOS || UNITY_EDITOR
	public static int setFilePermissions(string filePath, string _user, string _group, string _other){
		#if (UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_ANDROID || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX || UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR_WIN
			return lsetPermissions(filePath, _user, _group, _other);
		#else
			return -1;
		#endif
	}
    #endif


#if !UNITY_TVOS || UNITY_EDITOR

    //Helper function
    private static bool checkObject(object fileBuffer, string filePath, ref GCHandle fbuf, ref IntPtr fileBufferPointer, ref int fileBufferLength) {
		if(fileBuffer is byte[]) { byte[] tempBuf = (byte[])fileBuffer; fbuf = gcA(tempBuf); fileBufferPointer = fbuf.AddrOfPinnedObject(); fileBufferLength = tempBuf.Length; return true; }
		if(fileBuffer is IntPtr) { fileBufferPointer = (IntPtr)fileBuffer; fileBufferLength = Convert.ToInt32(filePath); }
        return false;
    }


    // A function to decompress a 7z file
    //
	// filePath			: the full path to the archive, including the archives name. (/myPath/myArchive.7z)
	// exctractionPath	: the path in where you want your files to be extracted. If null, the same path as of the 7z archive will be used.
	// progress         : a single item integer array to get the progress of the extracted files (use this function when calling from a separate thread, otherwise call the 2nd implementation)
    //                  : if you want byte level real time progress, call this function from a thread and use the getBytesRead or getBytesWritten to compare against the total uncompressed
    //                  : size of the files or against the file size of the 7z archive.
	// largeFiles		: set this to true if you are extracting files larger then 90-100 Mb. It is slower though but prevents crashing your app when extracting large files!
	// fullPaths		: set this to true if you want to keep the folder structure of the 7z file.
	// entry			: set the name of a single file file you want to extract from your archive. If the file resides in a folder, the full path should be added.
	//					   (for example  game/meshes/small/table.mesh )
	//                     For entry extractions, it is much faster if the 7z is non-solid compressed!
	// fileBuffer		: A buffer that holds a 7zip file. When assigned the function will decompress from this buffer and will ignore the filePath. (iOS, Android, MacOSX, Linux)
    //                  : It can be a byte[] buffer or a native IntPtr buffer (downloaded using the helper function: download7zFileNative)
    //                  : When an IntPtr is used as the input buffer, the size of it must be passed to the function as a string with the filePath parameter.
    //
	// use this function from a separate thread to get the progress  of the extracted files in the referenced 'progress' integer, or use the getBytesRead/getBytesWritten function fro byte level % progress.
	//
	// ERROR CODES:
	//  1 : OK
	//	2 : Could not find requested file in archive
	// -1 : Could not open input(7z) file
	// -2 : Decoder doesn't support this archive
	// -3 : Can not allocate memory
	// -5 : Unknown error
	// -6 : File IO error

	public static int doDecompress7zip(string filePath, string exctractionPath = null,  int [] progress = null, bool largeFiles = false, bool fullPaths = true, string entry = null, object fileBuffer = null) {

        if(exctractionPath == null) exctractionPath = Path.GetDirectoryName(filePath);
		if (@exctractionPath.Substring(@exctractionPath.Length - 1, 1) != "/") { @exctractionPath += "/"; }
        if(!Directory.Exists(@exctractionPath)) Directory.CreateDirectory(@exctractionPath);
		if(entry == "") entry = null;

		int res = 0;
        if(progress == null) progress = new int[1];
		var ibuf = gcA(progress);

		#if (UNITY_IPHONE || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR_WIN

		if(fileBuffer != null) {
            int fileBufferLength = 0;
            IntPtr fileBufferPointer = IntPtr.Zero;
            GCHandle fbuf = gcA(null);
			bool managed = checkObject(fileBuffer, filePath, ref fbuf, ref fileBufferPointer, ref fileBufferLength);
            if (!managed && fileBufferLength == 0) { Debug.Log("Please provide a valid native buffer size as a string in filePath"); return -5; }

			if (largeFiles){
				res = decompress7zip(null, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject() ,fileBufferPointer , fileBufferLength);
			}else{
				res =  decompress7zip2(null, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject() , fileBufferPointer, fileBufferLength);
			}
				if (managed) fbuf.Free();
                ibuf.Free(); return res;
		} else {
			if (largeFiles){
				res = decompress7zip(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject() , IntPtr.Zero, 0);
				ibuf.Free(); return res;
			}else{
				res = decompress7zip2(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject() , IntPtr.Zero, 0);
				ibuf.Free(); return res;
			}
		}
		
		#endif
		
 		#if (!UNITY_EDITOR_OSX  && !UNITY_ANDROID && !UNITY_IOS && !UNITY_EDITOR_LINUX && !UNITY_STANDALONE_LINUX) || UNITY_EDITOR_WIN
            if (largeFiles){
				res = decompress7zip(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
				ibuf.Free(); return res;
			}else{
				res = decompress7zip2(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
				ibuf.Free(); return res;
			}
		#endif
    }


    // Same function as above only the progress integer is a local variable.
    // Use this when you don't want to get the progress of the extracted files and when not calling the function from a separate thread.
    public static int doDecompress7zip(string filePath, string exctractionPath = null,  bool largeFiles = false, bool fullPaths = true, string entry = null, object fileBuffer = null) {

        if(exctractionPath == null) exctractionPath = Path.GetDirectoryName(filePath);
        //make a check if the last '/' exists at the end of the exctractionPath and add it if it is missing
        if (@exctractionPath.Substring(@exctractionPath.Length - 1, 1) != "/") { @exctractionPath += "/"; }
        if(!Directory.Exists(@exctractionPath)) Directory.CreateDirectory(@exctractionPath);
		if(entry == "") entry = null;

        int[] progress = new int[1];
		var ibuf = gcA(progress);

		int res = 0;
		
		#if (UNITY_IPHONE || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR_WIN
		if(fileBuffer != null) {
            int fileBufferLength = 0;
            IntPtr fileBufferPointer = IntPtr.Zero;
            GCHandle fbuf = gcA(null);
			bool managed = checkObject(fileBuffer, filePath, ref fbuf, ref fileBufferPointer, ref fileBufferLength);
            if(!managed && fileBufferLength == 0) { Debug.Log("Please provide a valid native buffer size as a string in filePath"); return -5;}

			if (largeFiles){
				res = decompress7zip(null, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), fileBufferPointer , fileBufferLength);
			}else{
				res = decompress7zip2(null, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), fileBufferPointer , fileBufferLength);
			}
			    fbuf.Free();
                ibuf.Free(); return res;
		} else {
			if (largeFiles){
				res = decompress7zip(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
				ibuf.Free(); return res;
			}else{
				res = decompress7zip2(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
				ibuf.Free(); return res;
			}
		}
		#endif
		
		#if (!UNITY_EDITOR_OSX  && !UNITY_ANDROID && !UNITY_IOS && !UNITY_EDITOR_LINUX && !UNITY_STANDALONE_LINUX) || UNITY_EDITOR_WIN
			if (largeFiles){
				res = decompress7zip(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
				ibuf.Free(); return res;
            }
            else{
				res = decompress7zip2(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
				ibuf.Free(); return res;
            }
		#endif
    }



	// This function encodes a single archive in lzma alone format.
    //
	// inPath	: the file to be encoded. (use full path + file name)
	// outPath	: the .lzma file that will be produced. (use full path + file name)
	//
	// You can set the compression properties by calling the setProps function before.
	// setProps(9) for example will set compression level to highest level.
    //
    // To get the progress of compression, call this function from a thread and use the getBytesRead function to compare against the file size of the encoded archive.
    //
	// ERROR CODES (for both encode/decode LzmaUtil functions):
	//   1 : OK
	//  -1 : Can not read input file
	//  -2 : Can not write output file
	// -12 : Can not allocate memory
	// -13 : Data error
    // -11 : Can't write
    // -10 : Cant read	
    public static int LzmaUtilEncode(string inPath, string outPath){
		if (!defaultsSet) setProps();
		var prps = gcA(props);
		int res = lzmaUtil(true, @inPath, @outPath, prps.AddrOfPinnedObject());
		prps.Free();
		return res;
	}


	// This function decodes a single archive in lzma alone format.
    //
	// inPath	: the .lzma file that will be decoded. (use full path + file name)
	// outPath	: the decoded file. (use full path + file name)
    //
    // To get the progress of decompression, call this function from a thread and use the getBytesRead function to compare against the file size of the compressed archive.
    //
	// ERROR CODES (for both encode/decode LzmaUtil functions):
	//   1 : OK
	//  -1 : Can not read input file
	//  -2 : Can not write output file
	// -12 : Can not allocate memory
	// -13 : Data error
    // -11 : Can't write
    // -10 : Cant read	
	public static int LzmaUtilDecode(string inPath, string outPath){
		return lzmaUtil(false, @inPath, @outPath, IntPtr.Zero);
	}
    #endif

	// Lists that get filled with filenames (including path if the file is in a folder) and uncompressed file sizes by the get7zInfo function
	public static List <string> ninfo = new List<string>();//filenames
	public static List <ulong> sinfo = new List<ulong>();//file sizes

    // An integer variable to store the total number of files in a 7z archive, excluding the folders.
    public static int trueTotalFiles = 0;


    // This function fills the ArrayLists with the filenames and file sizes that are in the 7zip file
    //
    // returns			: the total size in bytes of the files in the 7z archive 
    //
    // filePath			: the full path to the archive, including the archives name. (/myPath/myArchive.7z)
	// fileBuffer		: A buffer that holds a 7zip file. When assigned the function will decompress from this buffer and will ignore the filePath. (iOS, Android, MacOSX, Linux)
    //                  : It can be a byte[] buffer or a native IntPtr buffer (downloaded using the helper function: download7zFileNative)
    //                  : When an IntPtr is used as the input buffer, the size of it must be passed to the function as a string with the filePath parameter.
    //
    // trueTotalFiles is an integer variable to store the total number of files in a 7z archive, excluding the folders.

    public static ulong get7zInfo(string filePath, object fileBuffer = null) {

        ninfo.Clear(); sinfo.Clear();
        trueTotalFiles = 0;

        IntPtr uni = IntPtr.Zero;

		#if (UNITY_IPHONE || UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR_WIN
		    if(fileBuffer != null) {
                int fileBufferLength = 0;
                IntPtr fileBufferPointer = IntPtr.Zero;
                GCHandle fbuf = gcA(null);
			    bool managed = checkObject(fileBuffer, filePath, ref fbuf, ref fileBufferPointer, ref fileBufferLength);
                if(!managed && fileBufferLength == 0) { Debug.Log("Please provide a valid native buffer size as a string in filePath"); return 0;}

                uni = _getSize(null, fileBufferPointer, fileBufferLength, false);
                fbuf.Free();
            }else {
                uni = _getSize(@filePath, IntPtr.Zero, 0, false);
            }
        #else
            uni = _getSize(@filePath,   IntPtr.Zero, 0, false);     
        #endif   

        if (uni == IntPtr.Zero) { /*Debug.Log("Input file not found.");*/ return 0; }

		string str = Marshal.PtrToStringAuto ( uni );
		StringReader r = new StringReader(str);
        string line;

        string[] rtt;
        ulong t = 0, sum = 0;
		
		while ((line = r.ReadLine()) != null) {
			rtt = line.Split('|');
			if(rtt.Length > 0) ninfo.Add(rtt[0]); else ninfo.Add("null");
			if(rtt.Length > 1) {
                ulong.TryParse(rtt[1], out t);
			    sum += t;
			    sinfo.Add(t);
			    if (t > 0) trueTotalFiles++;
            } else {
                sinfo.Add(0);
            }
		}

		r.Close();
		r.Dispose();
        _releaseBuffer(uni);

        return sum;
    }
    
	// This function returns the uncompressed file size of a given file in the 7z archive if specified, otherwise it will return the total uncompressed size of all the files in the archive.
	//
	// filePath			: the full path to the archive, including the archives name. (/myPath/myArchive.7z)
	// 					: if you call the function with filePath as null, it will try to find file sizes from the last call.
	// entry 			: the file name we want to get the file size (if it resides in a folder add the folder path also). If null the size of all the files will ne returned.
	// fileBuffer		: A buffer that holds a 7zip file. When assigned the function will decompress from this buffer and will ignore the filePath. (iOS, Android, MacOSX, Linux)
    //                  : It can be a byte[] buffer or a native IntPtr buffer (downloaded using the helper function: download7zFileNative)
    //                  : When an IntPtr is used as the input buffer, the size of it must be passed to the function as a string with the filePath parameter.
	public static ulong get7zSize( string filePath = null, string entry = null, object fileBuffer=null) {
		ulong sum = 0;
		if (entry == "") entry = null;

		    #if (UNITY_IPHONE || UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR_WIN
		        if(fileBuffer != null) {
                    int fileBufferLength = 0;
                    IntPtr fileBufferPointer = IntPtr.Zero;
                    GCHandle fbuf = gcA(null);
			        bool managed = checkObject(fileBuffer, filePath, ref fbuf, ref fileBufferPointer, ref fileBufferLength);
                    if(!managed && fileBufferLength == 0) { Debug.Log("Please provide a valid native buffer size as a string in filePath"); return 0;}

                    sum = entrySize(null, entry, fileBufferPointer, fileBufferLength);
                    fbuf.Free();
                    return sum;
                }else {
                    sum = entrySize(@filePath, entry, IntPtr.Zero, 0);
                    return sum;
                }
            #else
                sum = entrySize(@filePath, entry, IntPtr.Zero, 0);
                return sum;
            #endif 
	}

    // A function that return the headers size of a 7z archive.
    //
    // This function is usefull for getting the correct progress when extracting an entry.
    //
	// filePath			: the full path to the archive, including the archives name. (/myPath/myArchive.7z)
	// fileBuffer		: A buffer that holds a 7zip file. When assigned the function will decompress from this buffer and will ignore the filePath. (iOS, Android, MacOSX, Linux)
    //                  : It can be a byte[] buffer or a native IntPtr buffer (downloaded using the helper function: download7zFileNative)
    //                  : When an IntPtr is used as the input buffer, the size of it must be passed to the function as a string with the filePath parameter.
    public static uint getHeadersSize(string filePath,  object fileBuffer = null) {
        IntPtr res = IntPtr.Zero;
		#if (UNITY_IPHONE || UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR_WIN
		    if(fileBuffer != null) {
                int fileBufferLength = 0;
                IntPtr fileBufferPointer = IntPtr.Zero;
                GCHandle fbuf = gcA(null);
			    bool managed = checkObject(fileBuffer, filePath, ref fbuf, ref fileBufferPointer, ref fileBufferLength);
				if(!managed && fileBufferLength == 0) { Debug.Log("Please provide a valid native buffer size as a string in filePath"); return 0;}

                res = _getSize(null, fileBufferPointer, fileBufferLength, true);
                fbuf.Free();
            }else {
                res = _getSize(@filePath, IntPtr.Zero, 0, true);
            }
        #else
            res = _getSize(@filePath, IntPtr.Zero, 0, true);     
        #endif 

        if( res != IntPtr.Zero) _releaseBuffer(res);

        return (uint)getBytesRead();
    }

	// A function to decode a specific archive in a 7z archive to a byte buffer
	//
	// filePath		: the full path to the 7z archive 
	// entry		: the file name to decode to a buffer. If the file resides in a folder, the full path should be used.
	// fileBuffer		: A buffer that holds a 7zip file. When assigned the function will decompress from this buffer and will ignore the filePath. (iOS, Android, MacOSX, Linux)
    //                  : It can be a byte[] buffer or a native IntPtr buffer (downloaded using the helper function: download7zFileNative)
    //                  : When an IntPtr is used as the input buffer, the size of it must be passed to the function as a string with the filePath parameter.
	public static byte[] decode2Buffer(string filePath, string entry, object fileBuffer=null) {
		
		int bufs = (int)get7zSize( @filePath, entry, fileBuffer );
        if (bufs <= 0) return null;//entry error or it does not exist
        byte[] nb = new byte[bufs];
        int res = 0;
		if(entry == "") entry = null;

        var dec2buf = gcA(nb);

		#if (UNITY_IPHONE || UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR_WIN
		    if(fileBuffer != null) {
                int fileBufferLength = 0;
                IntPtr fileBufferPointer = IntPtr.Zero;
                GCHandle fbuf = gcA(null);
			    bool managed = checkObject(fileBuffer, filePath, ref fbuf, ref fileBufferPointer, ref fileBufferLength);
                if(!managed && fileBufferLength == 0) { Debug.Log("Please provide a valid native buffer size as a string in filePath"); return null;}

                res = decode2Buf(null, entry, dec2buf.AddrOfPinnedObject(), fileBufferPointer, fileBufferLength);
                fbuf.Free();
            }else {
                res = decode2Buf(@filePath, entry, dec2buf.AddrOfPinnedObject(), IntPtr.Zero, 0);
            }
        #else
            res = decode2Buf(@filePath, entry, dec2buf.AddrOfPinnedObject(), IntPtr.Zero, 0);    
        #endif

        dec2buf.Free();
		if(res == 1){ return nb;}
		else {nb = null; return null; }

    }

	#if !UNITY_TVOS || UNITY_EDITOR
	// ---------------------------------------------------------------------------------------------------------------------------------------------------------------
	//
	// UTILITY FUNCTIONS
	//
	// ---------------------------------------------------------------------------------------------------------------------------------------------------------------

	// Use this function to get the total files of a directory and its subdirectories.
	public static int getAllFiles(string dir) {
		string[] filePaths = Directory.GetFiles(@dir, "*", SearchOption.AllDirectories);
		int res = filePaths.Length;
		filePaths = null;
		return res;
	}

    // Use this function to get the size of a file in the file system.
	public static long getFileSize(string file) {
		FileInfo fi = new FileInfo(file);
        if(fi.Exists) return fi.Length; else return 0;
	}

    // Use this function to get the size of the files in a directory.
    public static ulong getDirSize(string dir) {
        string[] filePaths = Directory.GetFiles(@dir, "*", SearchOption.AllDirectories);
        ulong size = 0;
        for(int i = 0; i < filePaths.Length; i++) {
		    FileInfo fi = new FileInfo(filePaths[i]);
            if(fi.Exists) size += (ulong)fi.Length;
        }
        return size;
	}
    #endif


#endif

	// ---------------------------------------------------------------------------------------------------------------------------------------------------------------
	//
	// BUFFER FUNCTIONS
	//
	// ---------------------------------------------------------------------------------------------------------------------------------------------------------------


    // This function encodes inBuffer to lzma alone format into the outBuffer provided.
    // The buffer can be saved also into a file and can be opened by applications that opens the lzma alone format.
    // This buffer can be uncompressed by the decompressBuffer function.
    // Returns true if success
    //
    // if makeHeader == false then the lzma 13 bytes header will not be added to the buffer.
	//
	// You can set the compression properties by calling the setProps function before.
	// setProps(9) for example will set compression level to the highest level.
	//
    public static  bool compressBuffer(byte[] inBuffer, ref byte[] outBuffer, bool makeHeader=true){

        if (!defaultsSet) setProps();
        var prps = gcA(props);

		var cbuf = gcA(inBuffer);
		IntPtr ptr;
        
        int res = 0;

		ptr = Lzma_Compress(cbuf.AddrOfPinnedObject(), inBuffer.Length, makeHeader, ref res, prps.AddrOfPinnedObject());

		cbuf.Free(); prps.Free();

		if(res == 0 || ptr == IntPtr.Zero) {_releaseBuffer(ptr); return false;}

		Array.Resize(ref outBuffer,res);
		Marshal.Copy(ptr, outBuffer, 0, res);

		_releaseBuffer(ptr);

		return true;
	}

    // Same as the above function, only this returns the compressed buffer in a new created byte[] buffer.
    public static byte[] compressBuffer(byte[] inBuffer, bool makeHeader=true) {

        if (!defaultsSet) setProps();
        var prps = gcA(props);

		var cbuf = gcA(inBuffer);
		IntPtr ptr;
        
        int res = 0;

		ptr = Lzma_Compress(cbuf.AddrOfPinnedObject(), inBuffer.Length, makeHeader, ref res, prps.AddrOfPinnedObject());

		cbuf.Free(); prps.Free();

		if(res == 0 || ptr == IntPtr.Zero){_releaseBuffer(ptr); return null;}


        byte[] outBuffer = new byte[res];

		Marshal.Copy(ptr, outBuffer, 0, res);

		_releaseBuffer(ptr);

		return outBuffer;
	}

    // Same as the above function, only it compresses a part of the input buffer.
	//
	// inBufferPartialLength: the size of the input buffer that should be compressed
	// inBufferPartialIndex:  the offset of the input buffer from where the compression will start
	//
	public static bool compressBufferPartial(byte[] inBuffer, int inBufferPartialIndex, int inBufferPartialLength, ref byte[] outBuffer, bool makeHeader = true) {
		if(inBufferPartialIndex + inBufferPartialLength > inBuffer.Length) return false;

        if (!defaultsSet) setProps();
        var prps = gcA(props);
        var cbuf = gcA(inBuffer);

        IntPtr ptr;
        IntPtr ptrPartial;

        int res = 0;

        ptrPartial = new IntPtr(cbuf.AddrOfPinnedObject().ToInt64() + inBufferPartialIndex);

        ptr = Lzma_Compress(ptrPartial, inBufferPartialLength, makeHeader, ref res, prps.AddrOfPinnedObject());

        cbuf.Free();

        if (res == 0 || ptr == IntPtr.Zero) { _releaseBuffer(ptr); return false; }

        Array.Resize(ref outBuffer, res);
        Marshal.Copy(ptr, outBuffer, 0, res);
		 
        _releaseBuffer(ptr);

        return true;
    }


	// Same as compressBufferPartial, only this function will compress the data into a fixed size buffer
	// The compressed size is returned so you can manipulate it at will.
    //
    // Use the 'safe' parameter to avoid buffer overrun.
	public static int compressBufferPartialFixed(byte[] inBuffer, int inBufferPartialIndex, int inBufferPartialLength, ref byte[] outBuffer, bool safe = true,  bool makeHeader = true) {
		if(inBufferPartialIndex + inBufferPartialLength > inBuffer.Length) return 0;

        if (!defaultsSet) setProps();
        var prps = gcA(props);
        var cbuf = gcA(inBuffer);

        IntPtr ptr;
        IntPtr ptrPartial;

        int res = 0;

        ptrPartial = new IntPtr(cbuf.AddrOfPinnedObject().ToInt64() + inBufferPartialIndex);

        ptr = Lzma_Compress(ptrPartial, inBufferPartialLength, makeHeader, ref res, prps.AddrOfPinnedObject());

        cbuf.Free();

        if (res == 0 || ptr == IntPtr.Zero) { _releaseBuffer(ptr); return 0; }

		// if the compressed buffer is larger then the fixed size buffer we use:
		// 1. then write only the data that fit in it.
		// 2. or we return 0. 
		// It depends on if we set the safe flag to true or not.
		if(res > outBuffer.Length) {
			if(safe) { _releaseBuffer(ptr); return 0; } else {  res = outBuffer.Length; }
		}

        Marshal.Copy(ptr, outBuffer, 0, res);
		 
        _releaseBuffer(ptr);

        return res;
    }


	// Same as the compressBuffer function, only this function will put the result in a fixed size buffer to avoid memory allocations.
	// The compressed size is returned so you can manipulate it at will.
    //
    // Use the 'safe' parameter to avoid buffer overrun.
	public static int compressBufferFixed(byte[] inBuffer, ref byte[] outBuffer, bool safe = true, bool makeHeader=true) {

        if (!defaultsSet) setProps();
        var prps = gcA(props);

		var cbuf = gcA(inBuffer);
		IntPtr ptr;
        
        int res = 0;

		ptr = Lzma_Compress(cbuf.AddrOfPinnedObject(), inBuffer.Length, makeHeader, ref res, prps.AddrOfPinnedObject());

		cbuf.Free(); prps.Free();
		if(res == 0 || ptr == IntPtr.Zero){_releaseBuffer(ptr); return 0;}

		// if the compressed buffer is larger then the fixed size buffer we use:
		// 1. then write only the data that fit in it.
		// 2. or we return 0. 
		// It depends on if we set the safe flag to true or not.
		if(res > outBuffer.Length) {
			if(safe) { _releaseBuffer(ptr); return 0; } else {  res = outBuffer.Length; }
		}

		Marshal.Copy(ptr, outBuffer, 0, res);

		_releaseBuffer(ptr);

		return res;
	}



    // This function decompresses an lzma compressed byte buffer and puts the data in a provided referenced out-buffer that will get resized to fit the data.
    // If the useHeader flag is false you have to provide the uncompressed size of the buffer via the customLength integer.
    // if res == 0 operation was successful
    //
    // Error codes
    /*
        OK 0
		
        ERROR_DATA 1
        ERROR_MEM 2
        ERROR_UNSUPPORTED 4
        ERROR_PARAM 5
        ERROR_INPUT_EOF 6
        ERROR_OUTPUT_EOF 7
        ERROR_FAIL 11
        ERROR_THREAD 12
        */
    public static  int decompressBuffer(byte[] inBuffer,  ref byte[] outbuffer, bool useHeader = true, int customLength = 0) {
		
		var cbuf = gcA(inBuffer);
		int uncompressedSize = 0;
		
		//if the lzma header will be used to extract the uncompressed size of the buffer. If the buffer does not have a header 
		//provide the known uncompressed size through the customLength integer.
		if(useHeader) uncompressedSize = (int)BitConverter.ToUInt64(inBuffer,5); else uncompressedSize = customLength;

		Array.Resize(ref outbuffer, uncompressedSize);

		var obuf = gcA(outbuffer);
		
		int res = Lzma_Uncompress(cbuf.AddrOfPinnedObject(), inBuffer.Length, uncompressedSize, obuf.AddrOfPinnedObject(), useHeader);

		cbuf.Free();
		obuf.Free();

		//if(res!=0){/*Debug.Log("ERROR: "+res.ToString());*/ return res; }
	
		return res;		
	}

    // Same as above, only this one returns a new created buffer with the data
	public static  byte[] decompressBuffer(byte[] inBuffer, bool useHeader = true, int customLength = 0) {
		
		var cbuf = gcA(inBuffer);
		int uncompressedSize = 0;
		
		//if the lzma header will be used to extract the uncompressed size of the buffer. If the buffer does not have a header 
		//provide the known uncompressed size through the customLength integer.
		if(useHeader) uncompressedSize = (int)BitConverter.ToUInt64(inBuffer,5); else uncompressedSize = customLength;

		byte[] outbuffer = new byte[uncompressedSize];

		var obuf = gcA(outbuffer);
		
		int res = Lzma_Uncompress(cbuf.AddrOfPinnedObject(), inBuffer.Length, uncompressedSize, obuf.AddrOfPinnedObject(), useHeader);

		cbuf.Free();
		obuf.Free();

		if(res!=0){/*Debug.Log("ERROR: "+res.ToString());*/ return null; }
	
		return outbuffer;		
	}


	// Same as above function. Only this one outputs to a buffer of fixed which size isn't resized to avoid memory allocations.
	// The fixed buffer should have a size that will be able to hold the incoming decompressed data.
    //
	// returns the uncompressed size.
	public static  int decompressBufferFixed(byte[] inBuffer,  ref byte[] outbuffer, bool safe = true, bool useHeader = true, int customLength = 0) {

		int uncompressedSize = 0;
		
		// if the lzma header will be used to extract the uncompressed size of the buffer. If the buffer does not have a header 
		// provide the known uncompressed size through the customLength integer.
		if(useHeader) uncompressedSize = (int)BitConverter.ToUInt64(inBuffer,5); else uncompressedSize = customLength;

		// Check if the uncompressed size is bigger then the size of the fixed buffer. Then:
		// 1. write only the data that fit in it.
		// 2. or return a negative number. 
		// It depends on if we set the safe flag to true or not.
		if(uncompressedSize > outbuffer.Length) {
			if(safe) return -101;  else  uncompressedSize = outbuffer.Length;
		 }

		var cbuf = gcA(inBuffer);
		var obuf = gcA(outbuffer);
		
		int res = Lzma_Uncompress(cbuf.AddrOfPinnedObject(), inBuffer.Length, uncompressedSize, obuf.AddrOfPinnedObject(), useHeader);

		cbuf.Free();
		obuf.Free();

		if(res!=0){/*Debug.Log("ERROR: "+res.ToString());*/ return -res; }
	
		return uncompressedSize;		
	}


    #if !UNITY_WEBGL && !UNITY_TVOS
    // A reusable native memory pointer for downloading files.
    public static  IntPtr nativeBuffer = IntPtr.Zero;
    public static bool nativeBufferIsBeingUsed = false;
    public static int nativeOffset = 0;


    // A Coroutine to dowload a file to a native/unmaged memory buffer.
    // You can call it for an IntPtr.
    // 
    //
    // This function can only be called for one file at a time. Don't use it to call multiple files at once.
    // 
    // This is useful to avoid memory spikes when downloading large files and intend to decompress from memory.
    // With the old method, a copy of the downloaded file to memory would be produced by pinning the buffer to memory.
    // Now with this method, it is downloaded to memory and can be manipulated with no memory spikes.
    //
    // In any case, if you don't need the created in-Memory file, you should use the lzma._releaseBuffer function to free the memory!
    //
    // Parameters:
    //
    // url:             The url of the file you want to download to a native memory buffer.
    // downloadDone:    Informs a bool that the download of the file to memory is done.
    // pointer:         An IntPtr for a native memory buffer
    // fileSize:        The size of the downloaded file will be returned here.
    public static IEnumerator download7zFileNative(string url, Action<bool> downloadDone, Action<IntPtr> pointer = null, Action<int> fileSize = null) {
        // Get the file lenght first, so we create a correct size native memory buffer.
        UnityWebRequest wr = UnityWebRequest.Head(url);

        nativeBufferIsBeingUsed = true;

        yield return wr.SendWebRequest();
        string size = wr.GetResponseHeader("Content-Length");

        nativeBufferIsBeingUsed = false;

        #if UNITY_2020_1_OR_NEWER
        if (wr.result ==  UnityWebRequest.Result.ConnectionError || wr.result == UnityWebRequest.Result.ProtocolError) {
        #else
        if (wr.isNetworkError || wr.isHttpError) {
        #endif
            Debug.LogError("Error While Getting Length: " + wr.error);
        } else {
            if (!nativeBufferIsBeingUsed) { 

                //get the size of the zip
                int zipSize = Convert.ToInt32(size);

                // If the zip size is larger then 0
                if (zipSize > 0) {

                    nativeBuffer = _createBuffer(zipSize);
                    nativeBufferIsBeingUsed = true;

                    // buffer for the download
                    byte[] bytes = new byte[2048];
                    nativeOffset = 0;

                    using (UnityWebRequest wwwSK = UnityWebRequest.Get(url)) {

                        // Here we call our custom webrequest function to download our archive to a native memory buffer.
                        wwwSK.downloadHandler = new CustomWebRequest2(bytes);
                        
                        yield return wwwSK.SendWebRequest();

                        if (wwwSK.error != null) {
                            Debug.Log(wwwSK.error);
                        } else {
                            downloadDone(true);

                            if(pointer != null) { pointer(nativeBuffer); fileSize(zipSize); }

                            //reset intermediate buffer params.
                            nativeBufferIsBeingUsed = false;
                            nativeOffset = 0;
                            nativeBuffer = IntPtr.Zero;

                            //Debug.Log("Custom download done");
                        }
                    }
                    
                }

            } else { Debug.LogError("Native buffer is being used, or not yet freed!"); }
        }
    }


    // A custom WebRequest Override to download data to a native-unmanaged memory buffer.
    public class CustomWebRequest2 : DownloadHandlerScript {

        public CustomWebRequest2()
            : base()
        {
        }

        public CustomWebRequest2(byte[] buffer)
            : base(buffer)
        {
        }

        protected override byte[] GetData() { return null; }


        protected override bool ReceiveData(byte[] bytesFromServer, int dataLength) {
            if (bytesFromServer == null || bytesFromServer.Length < 1) {
                Debug.Log("CustomWebRequest: Received a null/empty buffer");
                return false;
            }

            var pbuf = gcA(bytesFromServer);
            
            //Process byteFromServer
            _addToBuffer(nativeBuffer, nativeOffset, pbuf.AddrOfPinnedObject(), dataLength );
            nativeOffset += dataLength;
            pbuf.Free();
            
            return true;
        }

        // Use the below functions only when needed. You get the same functionality from the main coroutine.
        /*
        // If all data has been received from the server
        protected override void CompleteContent()
        {
            //Debug.Log(Download Complete.");
        }

        // If a Content-Length header is received from the server.
        protected override void ReceiveContentLength(int fileLength)
        {
             //Debug.Log("ReceiveContentLength: " + fileLength);
        }
        */
    }
    #endif

#endif
}

