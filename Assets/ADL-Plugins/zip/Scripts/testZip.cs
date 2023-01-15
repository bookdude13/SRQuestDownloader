using UnityEngine;
using System;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class testZip : MonoBehaviour
{
#if (!UNITY_WEBGL && !UNITY_TVOS) || UNITY_EDITOR

	// we use some integer to get error codes from the lzma library (look at lzma.cs for the meaning of these error codes)
	private int zres=0;
	
	private string myFile;
    private string log;
	private string ppath;
	
	private bool compressionStarted, pass;
	private bool downloadDone, downloadDone2;

	// reusable buffers
    private byte[] reusableBuffer, reusableBuffer2, reusableBuffer3;

	// fixed size buffers, that don't get resized, to perform compression/decompression of buffers in them and avoid memory allocations.
	private byte[] fixedInBuffer = new byte[1024 * 256];
	private byte[] fixedOutBuffer = new byte[1024 * 768];
	private byte[] fixedBuffer = new byte[1024 * 1024];

    // A single item integer array that changes to the current number of file that get uncompressed of a zip archive.
    // When running the decompress_File function, compare this int to the total number of files returned by the getTotalFiles function
    // to get the progress of the extraction if the zip contains multiple files.
    // If you use multiple threads, remember to use other progress integers for the other threads, otherwise there will be a sharing violation.
    //
    private int[] progress = new int[1];

	// individual file progress (in bytes)
	private ulong[] progress2 = new ulong[1], byteProgress = new ulong[1];

    
    // log for output of results
    void plog(string t = "")
    {
        log += t + "\n"; ;
    }

    void Start() {

        ppath = Application.persistentDataPath;

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
		     ppath=".";
#endif

        Debug.Log("persistentDataPath: " + ppath);

        // various byte buffers for testing
        reusableBuffer = new byte[4096];
        reusableBuffer2 = new byte[0];
        reusableBuffer3 = new byte[0];

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // call the download coroutine to download a test file
        StartCoroutine(DownloadZipFile());
    }


	void Update(){
		if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
	}
	
	
	void OnGUI(){
	
        if(GUI.Button(new Rect(Screen.width - 100, 90, 80, 40), "Cancel")) lzip.setCancel();

		if (downloadDone == true) {
			GUI.Label(new Rect(10, 2, 250, 30), "package downloaded, ready to extract");
			GUI.Label(new Rect(310, 2, 650, 100), "Path: " + ppath);
		}
		
		if (compressionStarted || downloadDone2){
            GUI.TextArea(new Rect(10, 160, Screen.width - 20, Screen.height - 170), log);
            GUI.Label(new Rect(Screen.width - 30, 0, 80,40), progress[0].ToString());
			GUI.Label(new Rect(Screen.width - 140, 0, 80,40), progress2[0].ToString());
		}

        if (downloadDone) {
            // ZIP TEST
		    if (GUI.Button(new Rect(10, 40, 110, 50), "Zip test")) {
                log = "";
			    compressionStarted = true;
			    DoDecompression();
		    }

            //FILE BUFFER TEST
			#if (UNITY_IPHONE || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_EDITOR  || UNITY_STANDALONE_WIN)
				if (GUI.Button(new Rect(130, 40, 110, 50), "FileBuffer test")) {
					log = "";
					compressionStarted = true;
					DoDecompression_FileBuffer();
				}
                if (GUI.Button(new Rect(10, 100, 180, 50), "Native FileBuffer tests")) {
                    compressionStarted = true; downloadDone2 = false;
                    StartCoroutine( NativeFileBufferDownload() );
                }
            #endif

            // INMEMORY TEST
            if (GUI.Button(new Rect(250, 40, 110, 50),"InMemory Test")) {
                 log = "";
			    compressionStarted = true;
                DoInMemoryTest();
            }

            //MERGED ZIP TEST
            if(GUI.Button(new Rect(370, 40, 110, 50),"Merged zip Test")) {
                 log = "";
			    compressionStarted = true;
                DoDecompression_Merged();
            }

            //GZIP/BZIP2 TEST
            if(GUI.Button(new Rect(490, 40, 110, 50),"Gzip/Bz2 Test")) {
                log = "";
                compressionStarted = true;
                DoGzipBz2Tests();
            }

            //TAR TAR.GZ TAR.BZ2 TEST
            if(GUI.Button(new Rect(610, 40, 110, 50),"Tar Test")) {
                log = "";
                compressionStarted = true;
                DoTarTests();
            }
        }
		
	}



    #region Normal Zip tests
    // Test zip functions
    void DoDecompression(){

        //----------------------------------------------------------------------------------------------------------------
        // Commented out example on how to set the permissions of a MacOSX executable that has been unzipped so it can run.
        //
        //lzip.setFilePermissions(ppath + "/Untitled.app", "rwx","rx","rx");
        //lzip.setFilePermissions(ppath + "/Untitled.app/Contents/MacOS/Untitled", "rwx","rx","rx");
        //
        //----------------------------------------------------------------------------------------------------------------
        

        //---------------------------------------------------------------------------------------------------------------------------------------------------------
        //
        // Use the lzip.setCancel() command to cancel the compress_File, compressDir, compress_File_List, decompress_File, extract_entry, entry2Buffer functions.
        //
        //---------------------------------------------------------------------------------------------------------------------------------------------------------


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //
        // To get a progress percentage:
        //
        // Use the lzip.getFileInfo function. This function returns the total uncompressed bytes of the contents in the zip (among other things).
        // Then when decompressing (through a separate thread), divide the progress[0] returned by the function with the total bytes returned from the lzip.getFileInfo function.
        // Like this (float)(totalBytes / progress[0]). This will go from 0.0f to 1.0f. Multiply this with 100 and you will have the progress.
        //
        // The same for compression. Only this time you have to make the sum of the total file sizes and compare against the byteProgress of the compress_File/compressDir/compress_File_List function
        //
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
       
        // In general keeping the default utf-8 encoding should be ok. But in some cases where the application that created the zip didn't used utf-8, you should set your encoding with this function.
        // lzip.setEncoding(65001); // UTF-8
        // lzip.setEncoding(54936); // Chinese

		// validate sanity of a zip archive
		plog("Validate: "+ lzip.validateFile(ppath + "/testZip.zip").ToString());

        // decompress the downloaded file
        zres = lzip.decompress_File(ppath + "/testZip.zip", ppath+"/", progress,null, progress2);
		plog("decompress: "+zres.ToString());
		plog();

 
        // get the true total files of the zip
        plog("true total files: "+lzip.getTotalFiles(ppath + "/testZip.zip"));


        // get the total entries of the zip
        plog("true total entries: "+lzip.getTotalEntries(ppath + "/testZip.zip"));


        // entry exists
        bool eres = lzip.entryExists(ppath + "/testZip.zip", "dir1/dir2/test2.bmp");
        plog("entry exists: " + eres.ToString());

       
        // get entry dateTime
        plog();
        plog("DateTime: " + lzip.entryDateTime(ppath + "/testZip.zip", "dir1/dir2/test2.bmp").ToString());


        // extract an entry
        zres = lzip.extract_entry(ppath + "/testZip.zip", "dir1/dir2/test2.bmp", ppath + "/test22P.bmp", null, progress2);
        plog("extract entry: " + zres.ToString());
        plog();

        // extract entries using a List
        List<string> entries = new List<string>();
        entries.Add("dir1/dir2/test2.bmp");
        entries.Add("dir1/dir2/dir3/Unity_1.jpg");
        zres = lzip.extract_entries(ppath + "/testZip.zip", entries.ToArray(), ppath + "/entries", null, progress2);
        plog("extract entries: " + zres.ToString());
        plog();
        

        // compress a file and add it to a new zip
        //
        // we use the byteProgress var to monitor how many bytes have been processed. (useful for progress of compression.)
        zres = lzip.compress_File(9, ppath + "/test2Zip.zip", ppath + "/dir1/dir2/test2.bmp",false, "dir1/dir2/test2.bmp", null, null, false, 0, byteProgress);
        plog("compress: " + zres.ToString());

        // append a file to it
        // Appending to an existing file can be slow. For faster results, multiple files should be added using the compressDir or compress_File_List functions !
        //
        // for the second file we use compression level = 0, to test the STORE method.
        zres = lzip.compress_File(0, ppath + "/test2Zip.zip", ppath + "/dir1/dir2/dir3/Unity_1.jpg",true, "dir1/dir2/dir3/Unity_1.jpg","ccc",null, false, 0, byteProgress);
        plog("append: " + zres.ToString() + "\nTotal bytes processed: " + byteProgress[0].ToString());
        //reset byteProgress[0] to zero
        byteProgress[0] = 0;

        // localOffset operations
        //
        // the second file was added with compression level = 0. That means the method for it is STORE.
        // we use the lzip.localOffset list to retrieve its position in the zip file and extract it without zip operations.
        // this is usefull when you want to use the AssetBundle.LoadFromFile on a zip or use the zip container as a storage for non compressed files.
        //
        // first we get the info about the zip and fill the appropriate lists. You can use either lzip.getFileInfo or the lzip.getZipInfo(not working on zip64 zips) functions.
        lzip.getFileInfo(ppath + "/test2Zip.zip");

        // then we get the offset to the uncompressed data and the uncompressed size of the file.
        // get the index of our entry in the info lists
        int index = lzip.getEntryIndex("dir1/dir2/dir3/Unity_1.jpg");
        
        // if we got a valid index we are going to extract our archive without zip operations
        if(index != -1) {
            int uncompressedSize = (int)lzip.uinfo[index];
            int localOffset = (int)lzip.localOffset[index] + 30 + lzip.ninfo[index].Length; // for archives with the STORE method we use 30 bytes + name.length extra offset
            plog("Real Offset: " + localOffset.ToString());
            // create a temp byte buffer to store our archive
            byte[] tempBuffer = new byte[uncompressedSize];
            using (BinaryReader bnr = new BinaryReader(new FileStream(ppath + "/test2Zip.zip", FileMode.Open))) {
                bnr.BaseStream.Seek(localOffset, SeekOrigin.Begin);
                bnr.Read(tempBuffer, 0, uncompressedSize);
            }
            // write the file to disk to validate the operation
            File.WriteAllBytes(ppath + "/Offset.jpg", tempBuffer);
            tempBuffer = null;
        }

        
        plog();


        //------------------------------------------------------------------------------------------------------------------------------
        // SPANNED archives
        //------------------------------------------------------------------------------------------------------------------------------
        progress2[0] = 0;
        zres = lzip.compress_File(9, ppath + "/test2ZipSPAN.zip", ppath + "/dir1/dir2/test2.bmp", false, "dir1/dir2/test2.bmp", null, null, false, 20000, progress2);

        
        plog("compress SPAN: " + zres.ToString() + "  progress: " + progress2[0].ToString());

        // add a second file in the split disk archive
		zres = lzip.compress_File(9, ppath + "/test2ZipSPAN.zip", ppath + "/dir1/dir2/dir3/Unity_1.jpg", true, "dir1/dir2/dir3/Unity_1.jpg", null, null, false, 20000, progress2);
		
        plog("compress SPAN 2: " + zres.ToString() + "  progress: " + progress2[0].ToString());

        // decompress spanned archive
        progress2[0] = 0;
        zres = lzip.decompress_File(ppath + "/test2ZipSPAN.zip", ppath + "/SPANNED/", progress, null, progress2);
        plog("decompress SPAN: " + zres.ToString() + "  progress: " + progress2[0].ToString());


        plog();

        //------------------------------------------------------------------------------------------------------------------------------

        bool useBz2 = true;
        // macos/ios/tvos do not support bz2 compression method.
        #if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS)
        useBz2 = false;
        #endif

        // compress multiple files added in some lists, and protect them with a password
        //
        // create a list of files to get compressed
        List<string> myFiles = new List<string>();
        myFiles.Add(ppath + "/test22P.bmp");
        myFiles.Add(ppath + "/dir1/dir2/test2.bmp");
        // create an optional list with new names for the above listings
        List<string> myNames = new List<string>();
        myNames.Add("NEW_test22P.bmp");
        myNames.Add("dir13/dir23/New_test2.bmp");
        
        // use password and bz2 method
        zres = lzip.compress_File_List(9, ppath+"/fileList.zip", myFiles.ToArray(), progress, false, myNames.ToArray(),"password", useBz2);
        plog("MultiFile Compress password: " + zres.ToString());
        myFiles.Clear(); myFiles = null; myNames.Clear(); myNames = null;

        // decompress a password protected archive
        zres = lzip.decompress_File(ppath + "/fileList.zip", ppath+"/", progress,null, progress2,"password");
        plog("decompress password: " + zres.ToString());
                   
        plog();


        // compress a buffer to a file and name it.
        plog( "Buffer2File: "+ lzip.buffer2File(9, ppath + "/test3Zip.zip", "buffer.bin", reusableBuffer).ToString());

        // compress a buffer, name it and append it to an existing zip archive
        plog("Buffer2File append: " + lzip.buffer2File(9, ppath + "/test3Zip.zip", "dir4/buffer.bin", reusableBuffer, true).ToString());
        // Debug.Log(reusableBuffer.Length);
        plog();

        

        // get the uncompressed size of a specific file in the zip archive
        plog("get entry size: " + lzip.getEntrySize(ppath + "/testZip.zip", "dir1/dir2/test2.bmp").ToString());
        plog();

        
        // extract a file in a zip archive to a byte buffer (referenced buffer method)
        plog("entry2Buffer1: "+ lzip.entry2Buffer(ppath + "/testZip.zip","dir1/dir2/test2.bmp",ref reusableBuffer2).ToString() );
        // File.WriteAllBytes(ppath + "/out.bmp", reusableBuffer2);
        plog();

        // extract an entry in a zip archive to a fixed size buffer
        plog("entry2FixedBuffer: " + lzip.entry2FixedBuffer(ppath + "/testZip.zip", "dir1/dir2/test2.bmp", ref fixedBuffer).ToString());
        plog();


        // extract a file in a zip archive to a byte buffer (new buffer method)
        var newBuffer = lzip.entry2Buffer(ppath + "/testZip.zip", "dir1/dir2/test2.bmp");
        zres = 0;
        if (newBuffer != null) zres = 1;
        plog("entry2Buffer2: "+ zres.ToString());
        // write a file out to confirm all was ok
        //File.WriteAllBytes(ppath + "/out.bmp", newBuffer);
        plog();


        // FIXED BUFFER FUNCTIONS:
        int compressedSize = lzip.compressBufferFixed(newBuffer, ref fixedInBuffer, 9);
        plog(" # Compress Fixed size Buffer: " + compressedSize.ToString());

        if(compressedSize > 0){
            int decommpressedSize = lzip.decompressBufferFixed(fixedInBuffer, ref fixedOutBuffer);
            if(decommpressedSize > 0) plog(" # Decompress Fixed size Buffer: " + decommpressedSize.ToString());
        }
        plog();


        // compress a buffer into a referenced buffer
        pass = lzip.compressBuffer(reusableBuffer2, ref reusableBuffer3, 9);
        plog("compressBuffer1: " + pass.ToString());
        // write a file out to confirm all was ok
        if(pass) File.WriteAllBytes(ppath + "/out.bin", reusableBuffer3);


        // compress a buffer and return a new buffer with the compresed data.
        newBuffer = lzip.compressBuffer(reusableBuffer2,9);
        zres = 0;
        if (newBuffer != null) zres = 1;
        plog("compressBuffer2: " + zres.ToString());
        plog();


        // decompress a previously compressed buffer into a referenced buffer
        pass = lzip.decompressBuffer(reusableBuffer3, ref reusableBuffer2);
        plog("decompressBuffer1: " + pass.ToString());
        //Debug.Log(reusableBuffer2.Length);
        // write a file out to confirm all was ok
        if(pass) File.WriteAllBytes(ppath + "/out.bmp", reusableBuffer2);
        zres = 0;
        if (newBuffer != null) zres = 1;


        // decompress a previously compressed buffer into a new returned buffer
        newBuffer = lzip.decompressBuffer(reusableBuffer3);
        if(newBuffer != null) plog("decompressBuffer2: " + newBuffer.Length.ToString()); else plog("decompressBuffer2: Failed");
        if(newBuffer != null) File.WriteAllBytes(ppath + "/out2.bmp", newBuffer);
        plog();


        // get file info of the zip file (names, uncompressed and compressed sizes)
        plog( "total bytes: " + lzip.getFileInfo(ppath + "/testZip.zip").ToString());



        // Look through the ninfo, uinfo and cinfo Lists where the file names and sizes are stored.
        if (lzip.ninfo != null) {
            for (int i = 0; i < lzip.ninfo.Count; i++) {
                log += lzip.ninfo[i] + " - " + lzip.uinfo[i] + " / " + lzip.cinfo[i] + "\n";
            }
        }
        plog();


        // Recursively compress a directory
        int[] dirProgress = new int[1];
        lzip.compressDir(ppath + "/dir1", 9, ppath + "/recursive.zip", false, dirProgress);
        plog("recursive - no. of files: " + dirProgress[0].ToString());

        // decompress the above compressed zip to make sure all was ok.
        zres = lzip.decompress_File(ppath + "/recursive.zip", ppath+"/recursive/", progress, null, progress2);
        plog("decompress recursive: "+zres.ToString());


        // multithreading example to show progress of extraction, using the ref progress int
        // in this example it happens to fast, because I didn't want the user to download a big file with many entrie.
        Thread th = new Thread(decompressFunc); th.Start();


        // delete/replace entry example
        if(File.Exists(ppath+"/test-Zip.zip")) File.Delete(ppath+"/test-Zip.zip");
        if(File.Exists(ppath+"/testZip.zip")) File.Copy(ppath+"/testZip.zip", ppath+"/test-Zip.zip");



        // replace an entry with a byte buffer
        var newBuffer3 = lzip.entry2Buffer(ppath + "/testZip.zip", "dir1/dir2/test2.bmp");
        plog("replace entry: "+lzip.replace_entry(ppath+"/test-Zip.zip", "dir1/dir2/test2.bmp", newBuffer3, 9, null).ToString() );


        // replace an entry with a file in the disk (ability to asign a password or bz2 compression)
        plog("replace entry 2: "+lzip.replace_entry(ppath+"/test-Zip.zip", "dir1/dir2/test2.bmp",ppath+"/dir1/dir2/test2.bmp", 9, null, null).ToString() );


        // delete an entry in the zip
        plog("delete entry: "+lzip.delete_entry(ppath+"/test-Zip.zip", "dir1/dir2/test2.bmp").ToString() );

        
    }

    void decompressFunc()
    {
        int res = lzip.decompress_File(ppath + "/recursive.zip", ppath + "/recursive/", progress,null, progress2);
        if (res == 1) plog("multithreaded ok"); else plog("multithreaded error: "+res.ToString());
    }

    #endregion

    #region Native File Buffer tests
    // A function to download a zip archive to a native/unmanaged memory buffer
    // We use here a temp lzip.inMemory struct to keep the pointer and size of the buffer
    IEnumerator NativeFileBufferDownload() {

        //make a check that the intermediate native buffer is not being used!
        if(lzip.nativeBufferIsBeingUsed) { Debug.Log("Native buffer download is in use"); yield break; }

        log = "";

        // An inMemory lzip struct class.
        lzip.inMemory inMemZip = null;

        // download a file to a native/unmanaged memory buffer (to avoid memory spikes when decompressing from memory)
        // You can pass to the function an inMemory class or an IntPtr.

        // Here we are calling the coroutine for an inMemory class. (make sure your file server supports GetResponseHeader("Content-Length"))
        StartCoroutine(lzip.downloadZipFileNative("http://telias.free.fr/temp/" + myFile, r => downloadDone2 = r, result => inMemZip = result));

        plog("Downloading 1st file");
        plog();


        while (!downloadDone2) yield return true;

        
        if (inMemZip.info == null) {
            log = ""; plog("InMemZip is null. Please redownload to memory.");
        } else {
            plog("File size: " + inMemZip.size().ToString()); // show size of the zip


            // Here we pass directly the size of the buffer (in the place of the zip archive name) and the pointer of the buffer.
            zres = lzip.decompress_File(inMemZip.size().ToString(), ppath + "/native1/", progress, inMemZip.memoryPointer(), progress2);

            plog("Validate 1 " + lzip.validateFile(inMemZip.size().ToString(), inMemZip.memoryPointer()).ToString());

            plog("decompress native 1: " + zres.ToString());
            plog();

            // Here we pass the temp lzip.inMemory struct to the lzip.decompress_File function
            zres = lzip.decompress_File(null, ppath + "/native2/", progress, inMemZip, progress2);

            plog("Validate 2 " + lzip.validateFile(null, inMemZip).ToString());

            plog("decompress native 2: " + zres.ToString());
            plog();


            // extract entries using a List
            List<string> entries = new List<string>();
            entries.Add("dir1/dir2/test2.bmp");
            entries.Add("dir1/dir2/dir3/Unity_1.jpg");
            zres = lzip.extract_entries(inMemZip.size().ToString(), entries.ToArray(), ppath + "/entriesNative", inMemZip.memoryPointer(), progress2);
            plog("extract entries: " + zres.ToString());
            plog();

            // free the struct and the native memory it occupies!!!
            lzip.free_inmemory(inMemZip);
        }


        downloadDone2 = false;

        // A native memory pointer
        IntPtr nativePointer = IntPtr.Zero;

        // Declare an int to get the downloaded archive size from the coroutine. When we pass directly a pointer to the plugin we need the size or the buffer it occupies.
        int zipSize = 0;

        plog();
        plog("Downloading 2nd file");
        plog();

        // Here we are calling the coroutine for a pointer. We also get the downloaded file size. (make sure your file server supports GetResponseHeader("Content-Length"))
        StartCoroutine(lzip.downloadZipFileNative("http://telias.free.fr/temp/" + myFile, r => downloadDone2 = r, null, pointerResult => nativePointer = pointerResult, size => zipSize = size));

        while (!downloadDone2) yield return true;

        plog("File size: " + zipSize.ToString()); // show size of the zip

        // We pass directly the size of the buffer (in the place of the zip archive name) and the pointer of the buffer.
        zres = lzip.decompress_File(zipSize.ToString(), ppath + "/native3/", progress, nativePointer, progress2);

        plog("Validate 3 " + lzip.validateFile(zipSize.ToString(), nativePointer).ToString());

        plog();
        plog("decompress native 3: " + zres.ToString());
        plog();

        // Delete the buffer from native memory.
        lzip.releaseBuffer(nativePointer);
    }
    #endregion

    #region File Buffer tests
    // these functions demonstrate how to extract data from a zip file in a byte buffer.
    void DoDecompression_FileBuffer() {
		#if (UNITY_IPHONE || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_EDITOR || UNITY_STANDALONE_WIN)
			// we read a downloaded zip from the Persistent data path. It could be also a file in a www.bytes buffer.
			var fileBuffer = File.ReadAllBytes(ppath + "/testZip.zip");

			plog("Validate: "+ lzip.validateFile(null, fileBuffer).ToString());


			// decompress the downloaded file
			zres = lzip.decompress_File(null, ppath+"/", progress, fileBuffer, progress2);
			plog("decompress: " + zres.ToString() + "  progress: " + progress2[0]);


			plog("true total files: " + lzip.getTotalFiles(null, fileBuffer) );
			plog("total entries: " + lzip.getTotalEntries(null, fileBuffer) );


			// entry exists
			bool eres = lzip.entryExists(null, "dir1/dir2/test2.bmp", fileBuffer);
			plog("entry exists: " + eres.ToString());


			// extract an entry
			zres = lzip.extract_entry(null, "dir1/dir2/test2.bmp", ppath + "/test22B.bmp", fileBuffer, progress2);
			plog("extract entry: " + zres.ToString() + "  progress: " + progress2[0]);
			plog();

            // extract entries using a List
            List<string> entries = new List<string>();
            entries.Add("dir1/dir2/test2.bmp");
            entries.Add("dir1/dir2/dir3/Unity_1.jpg");
            zres = lzip.extract_entries(null, entries.ToArray(), ppath + "/entriesFB", fileBuffer, progress2);
            plog("extract entries: " + zres.ToString());
            plog();


			// get the uncompressed size of a specific file in the zip archive
			plog("get entry size: " + lzip.getEntrySize(null, "dir1/dir2/test2.bmp", fileBuffer).ToString());
			plog();


			// extract a file in a zip archive to a byte buffer (referenced buffer method)
			plog("entry2Buffer1: "+ lzip.entry2Buffer(null,"dir1/dir2/test2.bmp",ref reusableBuffer2, fileBuffer).ToString() );
		    //File.WriteAllBytes(ppath + "/out.bmp", reusableBuffer2);


			// extract a file in a zip archive to a byte buffer (new buffer method)
			var newBuffer = lzip.entry2Buffer(null, "dir1/dir2/test2.bmp", fileBuffer);
			zres = 0;
			if (newBuffer != null) zres = 1;
			plog("entry2Buffer2: "+ zres.ToString());
			// write a file out to confirm all was ok
			// File.WriteAllBytes(ppath + "/out.bmp", reusableBuffer2);
			plog();

			
			// get file info of the zip file (names, uncompressed and compressed sizes)
			plog( "total bytes: " + lzip.getFileInfo(null, fileBuffer).ToString());

			// Look through the ninfo, uinfo and cinfo Lists where the file names and sizes are stored.
			if (lzip.ninfo != null) {
				for (int i = 0; i < lzip.ninfo.Count; i++) {
					log += lzip.ninfo[i] + " - " + lzip.uinfo[i] + " / " + lzip.cinfo[i] + "\n";
				}
			}
			plog();

		#endif
	}
    #endregion

    #region InMemory zip tests
    void DoInMemoryTest() {
        // make sure we have some files to work with
        if(!File.Exists(ppath + "/dir1/dir2/test2.bmp")) {
            lzip.decompress_File(ppath + "/testZip.zip");
        }
        if(reusableBuffer2.Length ==0 ) lzip.entry2Buffer(ppath + "/testZip.zip", "dir1/dir2/test2.bmp", ref reusableBuffer2);

        //------------------------------------------------------------------------------------------------------------------------------
        //IN MEMORY zip functions 
        //------------------------------------------------------------------------------------------------------------------------------

        // first we create an in memory object to reference our in memory zip
        // (it is important to delete this object later. see below.)
        lzip.inMemory t = new lzip.inMemory();

        // read a file to a buffer
        byte[] reusableBuffer2a = File.ReadAllBytes(ppath + "/dir1/dir2/dir3/Unity_1.jpg");

        // compress a buffer to an in memory zip using bz2 method.
        // The function returns the pointer to the in memory zip buffer. But you can get this also through t.pointer.
        lzip.compress_Buf2Mem(t,  9, reusableBuffer2a, "inmem/Unity_1.jpg", null,"1234", true);

        reusableBuffer2a = null;

        // print the in memory zip size in bytes.
        plog("inMemory zip size: " + t.size().ToString());

        // add a second buffer to the in memory zip. If you use the same inMemory object that already has created an inMemory zip file ,
        // it will append the next one to it, resizing it!
        // Appending on an existing inMemory zip is slow as more files are added to it.
        // For really fast inMemory zip creation the lower level inMemory functions should be used. (See below.)
        lzip.compress_Buf2Mem(t, 9, reusableBuffer2, "inmem/test.bmp", null, "1234", true);

        // print the in memory zip size in bytes.
        plog("inMemory zip size: " + t.info[0].ToString());

        // Example on how to save an in memory zip file to disk.
        //
        // use the struct public function to get the byte buffer where the inmemory zip was created
        var p = t.getZipBuffer();

        // and save the buffer to disk.
        File.WriteAllBytes(ppath+"/MEM.zip", p);
        p = null;

        progress2[0] = 0;

        // Example on how to decompress an in memory zip to disk
        plog("decompress_Mem2File: " + lzip.decompress_Mem2File(t, ppath+"/", null, progress2,"1234").ToString() + "  progress: " + progress2[0].ToString());

        // a function to get info from an in memory zip.
        lzip.getFileInfoMem(t);

        plog();

        // print out the name, uncompressed and compressed sizes.
        if (lzip.ninfo != null) {
            for (int i = 0; i < lzip.ninfo.Count; i++) {
                log += lzip.ninfo[i] + " - " + lzip.uinfo[i] + " / " + lzip.cinfo[i] + "\n";
            }
        }

        plog();

        // a buffer to perform decompression operations from an in memory zip to it.
        byte[] bf = null;

        // a function that decompresses an entry from an in memory zip to a buffer that will get resized to fit the output.
        plog("entry2BufferMem: " + lzip.entry2BufferMem(t,"inmem/test.bmp", ref bf, "1234").ToString());

        // a function that decompresses an entry from an in memory zip to a fixed size buffer.
        plog("entry2FixedBufferMem: " + lzip.entry2FixedBufferMem(t, "inmem/test.bmp", ref bf, "1234").ToString());

        // a function that decompresses an entry from an in memory zip to a new created buffer and returns it.
        var bt = lzip.entry2BufferMem(t, "inmem/test.bmp",  "1234");
        plog("entry2BufferMem new buffer: " + bt.Length.ToString());

        // (!) If you don't need anymore the inMemory object use the free_inmemory function to free the occupied memory by the zip (!)
        //
        lzip.free_inmemory(t);

        plog();
        // -----------------------------------------------------------------------------------------------------------------------------


        // Lower level inMemory functions for Faster! compressing multiple files/buffers to memory.

        // Create an in memory object to reference our in memory zip
        lzip.inMemory t2 = new lzip.inMemory();

        // Initiate an inMemory zip archive
        lzip.inMemoryZipStart(t2);

        // Read some file to a byte[] buffer (as an example only).
        reusableBuffer2a = File.ReadAllBytes(ppath + "/dir1/dir2/dir3/Unity_1.jpg");

        // Add a buffer as a first entry in the inMemory zip
        lzip.inMemoryZipAdd(t2, 9, reusableBuffer2a, "test.jpg");

        // Add a second buffer as the second entry.
        lzip.inMemoryZipAdd(t2, 9, reusableBuffer2, "directory/test.bmp");

        // !!! -> After finishing adding buffer/files in the inMemory zip we must close it <- !!! 
        lzip.inMemoryZipClose(t2);

        // Reopen the t2 inMemory zip archive and add 2 more files. They will automatically get appended.
        lzip.inMemoryZipStart(t2);
        lzip.inMemoryZipAdd(t2, 9, reusableBuffer2a, "newDir/test2.jpg");
        lzip.inMemoryZipAdd(t2, 9, reusableBuffer2, "directory2/test2.bmp");
        lzip.inMemoryZipClose(t2);

        // Write out the compressed size of the inMemory created zip
        plog("Size of Low Level inMemory zip: " + t2.size().ToString());

        // Write a file to disk to make sure that everything was done correct.
        File.WriteAllBytes(ppath+"/MEM2.zip", t2.getZipBuffer());

        // nullify the temp buffer
        reusableBuffer2a = null;

        // (!) If you don't need anymore the inMemory object use the free_inmemory function to free the occupied memory by the zip (!)
        lzip.free_inmemory(t2);

        plog();

        //------------------------------------------------------------------------------------------------------------------------------
        // END IN MEMORY zip functions 
        //------------------------------------------------------------------------------------------------------------------------------

    }
    #endregion

    #region Bz2 tests
    void DoGzipBz2Tests() {
        //-------------------------------------------------------------------------------------------------------------------------
        // GZIP TESTS
        //-------------------------------------------------------------------------------------------------------------------------
        
        // make sure we have some data in one byte[] buffer
        if(reusableBuffer2.Length < 1) lzip.entry2Buffer(ppath + "/testZip.zip","dir1/dir2/test2.bmp",ref reusableBuffer2);

        // create a buffer that will store the compressed gzip data.
        // it should be at least the size of the input buffer +18 bytes.
        var btt = new byte[reusableBuffer2.Length+18];

        // compress a buffer to gzip format and add gzip header and footer
        // returns total size of compressed buffer.
        //
        // set the last parameter to true if you want to include the compressed size in the gzip. (use the lzip.gzipCompressedSize to get it).
        int rr = lzip.gzip(reusableBuffer2,btt,10, true, true);
        plog("gzip compressed size: "+ rr);


        // create a buffer to store the compressed data (optional, if you want to write out a gzip file)
        var bt2 = new byte[rr];
        // copy the data to the new buffer
        Buffer.BlockCopy(btt, 0, bt2, 0, rr);

        // write a .gz file
        File.WriteAllBytes(ppath+"/test2.bmp.gz", bt2);

        // create a buffer to decompress a gzip buffer
        var bt3 = new byte[lzip.gzipUncompressedSize(bt2)];

        // decompress the gzip compressed buffer
        int gzres = lzip.unGzip(bt2, bt3);
        //int gzres = lzip.unGzip2(bt2, bt3); // same as UnGzip assuming the gzip buffer has a gzip header. 

        if(gzres > 0) { File.WriteAllBytes(ppath+"/test2GZIP.bmp", bt3); plog("gzip decompression: success " + gzres.ToString());}
        else {plog("gzip decompression error: " + gzres.ToString());}

        btt=null; bt2=null; bt3=null;
        plog();

        // gzip file system tests
        
        // create a ulong progress variable. For realtime report call the functions from a Thread.
        ulong[] gzipProgress = new ulong[1];
        // create a gzip archive
        plog("Gzip file creation: " + lzip.gzipFile(ppath + "/test2GZIP.bmp", ppath + "/Ftest2GZIP.bmp.gz", 10, gzipProgress).ToString() + "  progress: " + gzipProgress[0].ToString());

        // extract a gzip archive
        plog("Gzip file decompression: " + lzip.ungzipFile(ppath + "/Ftest2GZIP.bmp.gz", ppath + "/Ftest2GZIP.bmp", gzipProgress).ToString() + "  progress: " + gzipProgress[0].ToString());
        plog();

        //-------------------------------------------------------------------------------------------------------------------------
        // BZIP2 TESTS
        //-------------------------------------------------------------------------------------------------------------------------

        plog("bz2 creation: " + lzip.bz2Create( ppath + "/Ftest2GZIP.bmp", ppath + "/Ftest2GZIP.bmp.bz2", 9, gzipProgress).ToString() + "  progress: " + gzipProgress[0].ToString());
        plog("bz2 extract: " + lzip.bz2Decompress( ppath + "/Ftest2GZIP.bmp.bz2", ppath + "/Ftest2GZIP-Bz2.bmp", gzipProgress).ToString() + "  progress: " + gzipProgress[0].ToString());
        plog();
    }
    #endregion

    #region Tar tests
    // The tar tests
    void DoTarTests() {
        // make sure we have some files to work with
        if(!Directory.Exists(ppath + "/mergedTests")) DoDecompression_Merged();
        log = "";

        ulong [] tarProgress = new ulong[1];
        tarProgress[0] = 0;

        // Windows only (other platforms don't need this).
        #if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        lzip.setTarEncoding(65001);//CP_UTF8 = 65001  // CP_OEMCP/UNICODE = 1
        #endif

        plog("Create Tar: " + lzip.tarDir(ppath+"/mergedTests", ppath + "/out.tar", true, null, tarProgress).ToString());
        plog("processed: " + tarProgress[0]);
        plog();

        tarProgress[0] = 0;

        plog("Extract Tar: " + lzip.tarExtract(ppath + "/out.tar", ppath + "/tarOut", null, tarProgress).ToString());
        plog("processed: " + tarProgress[0]);
        plog();
        
        plog("Extract Tar entry: " + lzip.tarExtractEntry(ppath + "/out.tar", "mergedTests/dir1/dir2/test2.bmp", ppath + "/tarOut2", true, tarProgress).ToString());
        plog("Extract Tar entry absolute Path: " + lzip.tarExtractEntry(ppath + "/out.tar", "mergedTests/overriden.jpg", ppath + "/outTarAbsolute.jpeg", false, tarProgress).ToString());
        plog();

        plog("tar.gz creation: " + lzip.gzipFile( ppath + "/out.tar", ppath + "/out.tar.gz", 10).ToString());
        plog("tar.bz2 creation: " + lzip.bz2Create( ppath + "/out.tar", ppath + "/out.tar.bz2", 9).ToString());
        plog();

        lzip.getTarInfo(ppath + "/out.tar");

        if(lzip.ninfo != null && lzip.ninfo.Count > 0) {
            for(int i=0; i < lzip.ninfo.Count; i++){
                plog("Entry no: " + (i+1).ToString() + "   " + lzip.ninfo[i] +"  size: " + lzip.uinfo[i].ToString() );
            }
        }

    }
    #endregion

    #region Merged zip tests
    // The merged/hidden zip file tests.
    void DoDecompression_Merged() {

        // if the merged file does not exist, create it.
        // (create a combination of a jpg and a zip file attached to it from existing files).
        if(!File.Exists(ppath + "/merged.jpg")) {
            if(!File.Exists(ppath + "/dir1/dir2/dir3/Unity_1.jpg")){
                lzip.decompress_File(ppath + "/testZip.zip", ppath+"/", progress,null, progress2);
            }

            var bf1 = File.ReadAllBytes(ppath + "/dir1/dir2/dir3/Unity_1.jpg");
            var bf2 = File.ReadAllBytes(ppath + "/testZip.zip");

            byte[] fb = new byte[bf1.Length + bf2.Length];

            Array.Copy(bf1, 0, fb, 0, bf1.Length);
            Array.Copy(bf2, 0, fb, bf1.Length, bf2.Length);

            File.WriteAllBytes(ppath + "/merged.jpg", fb);

            bf1 = null; bf2 = null; fb = null;
        // if the merged file exists, perfom some operations with it.
        }
                
        // get entry info of the merged zip. (Can be used also for regular zip archives to get extended info on the entries. After calling this iterate through the lzip.zinfo List.)
        plog("Get Info of merged zip: " + lzip.getZipInfo(ppath + "/merged.jpg").ToString());

        // if the zip has entries
        if(lzip.zinfo != null && lzip.zinfo.Count > 0) {
            for(int i=0; i < lzip.zinfo.Count; i++){
                plog("Entry no: " + (i+1).ToString() + "   " + lzip.zinfo[i].filename +"  uncompressed: " + lzip.zinfo[i].UncompressedSize.ToString() + "  compressed: " + lzip.zinfo[i].CompressedSize.ToString());
            }
        }
        plog();

        // Extract the whole zip to disk
        int[] prog = new int[1];
        ulong[] byteProgress = new ulong[1];

        plog("Decompress to disk from merged file: " + lzip.decompressZipMerged(ppath + "/merged.jpg", ppath + "/mergedTests/",  prog, byteProgress).ToString() + " progress: " + byteProgress[0].ToString());

        // extract an entry from the merged zip that resides in the file system and override the entry name.
        plog("Extract entry to disk from merged file: " + lzip.entry2FileMerged(ppath + "/merged.jpg", "dir1/dir2/dir3/Unity_1.jpg", ppath + "/mergedTests", "overriden.jpg"));

        plog();
        // now let's say we have the above archive in a memory buffer.
        var tempBuffer = File.ReadAllBytes(ppath + "/merged.jpg");

        // get the zip entries from the merged archive in the buffer
        plog("Get Info of merged zip in Buffer: " + lzip.getZipInfoMerged(tempBuffer).ToString());

        if(lzip.zinfo != null && lzip.zinfo.Count > 0) {
            for(int i=0; i < lzip.zinfo.Count; i++){
                plog("Entry no: " + (i+1).ToString() + "   " + lzip.zinfo[i].filename +"  uncompressed: " + lzip.zinfo[i].UncompressedSize.ToString() + "  compressed: " + lzip.zinfo[i].CompressedSize.ToString());
            }
        }

        plog();

        // decompress to disk from a merged file that resides in a buffer.
        plog("Decompress to disk from merged buffer: " + lzip.decompressZipMerged(tempBuffer, ppath + "/mergedTests/",  prog).ToString());

        // extract an entry from the merged zip in the buffer to disk.
        plog("Entry2File from merged buffer: " + lzip.entry2FileMerged( tempBuffer, "dir1/dir2/dir3/Unity_1.jpg", ppath + "/mergedTests").ToString());

        plog();

        // extract an entry from a merged file in the file system to a new buffer.
        var newBuffer = lzip.entry2BufferMerged(ppath + "/merged.jpg", "dir1/dir2/dir3/Unity_1.jpg");
        plog("Size of entry in new buffer 1: " + newBuffer.Length);
        newBuffer = null;

        // extract an entry from a merged file in the file system to a fixed sized buffer.
        byte[] fixedSizedBuffer = new byte[11 * 1024];
        plog("Size of entry in fixed buffer 1: " + lzip.entry2FixedBufferMerged(ppath + "/merged.jpg", "dir1/dir2/dir3/Unity_1.jpg", ref fixedSizedBuffer).ToString());

        plog();
        // extract an entry from a merged file in a buffer to a new buffer.
        var newBuffer2 = lzip.entry2BufferMerged(tempBuffer, "dir1/dir2/dir3/Unity_1.jpg");
        plog("Size of entry in new buffer 2: " + newBuffer2.Length);

        // extract an entry from a merged file in a buffer to a fixed sized buffer.
        plog("Size of entry in fixed buffer 2: " + lzip.entry2FixedBufferMerged(tempBuffer, "dir1/dir2/dir3/Unity_1.jpg", ref fixedSizedBuffer).ToString());

        fixedSizedBuffer = null;
        tempBuffer = null;
    }
    #endregion



    IEnumerator DownloadZipFile() {

        myFile = "testZip.zip";

        // make sure a previous zip file having the same name with the one we want to download does not exist in the ppath folder
        if (File.Exists(ppath + "/" + myFile)) { downloadDone = true; yield break; }//File.Delete(ppath + "/" + myFile);

        Debug.Log("starting download");

        // replace the link to the zip file with your own (although this will work also)
        using (UnityWebRequest www = UnityWebRequest.Get("https://dl.dropboxusercontent.com/s/xve34ldz3pqvmh1/" + myFile)) {

            yield return www.SendWebRequest();

            if (www.error != null)
            {
                Debug.Log(www.error);
            } else {
                downloadDone = true;

                // write the downloaded zip file to the ppath directory so we can have access to it
                // depending on the Install Location you have set for your app, set the Write Access accordingly!
                File.WriteAllBytes(ppath + "/" + myFile, www.downloadHandler.data);

                Debug.Log("download done");
            }
        }
    }


#else
    void Start(){
        Debug.Log("Does not work on WebPlayer!");
    }
#endif

}

