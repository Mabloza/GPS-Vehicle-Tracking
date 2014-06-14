
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// KeyValidator.cs
//
// Symmetric-encryption IValidator implementation.
// 
// For more information see the Updater Application Block Implementation Overview. 
// 
//============================================================================================================
// Copyright (C) 2000-2001 Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR
// FITNESS FOR A PARTICULAR PURPOSE.
//============================================================================================================




using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml;


using Microsoft.ApplicationBlocks.ExceptionManagement;
using Microsoft.ApplicationBlocks.ApplicationUpdater.Interfaces;

namespace Microsoft.ApplicationBlocks.ApplicationUpdater.Validators
{
	/// <summary>
	/// The symmetric-key-based implementation of the IValidator interface.  Does not offer same level of security as the asymmetric
	/// RSA crypto.
	/// </summary>
	public class KeyValidator : IValidator
	{

		#region Declarations

		private byte[] _key;

		#endregion

		/// <summary>
		/// The default constructor
		/// </summary>
		public KeyValidator(){}


		/// <summary>
		/// Initialize the provider
		/// </summary>
		void IValidator.Init( XmlNode config )
		{			
			//  get the xml node that holds the key (might be empty)
			XmlNode keyNode = config.SelectSingleNode( "key" );

			//  look for key in xml
			_key = ExtractKeyFromNode( keyNode );

			//  we've tried to find key in "key" nodes, we should have a good key now; check to be sure
			if ( null == _key )
			{
				string error = ApplicationUpdateManager.TraceWrite( "[KeyValidator.Init]", "RES_CouldNotFindCryptographicKey" );

				CryptographicException ce = new CryptographicException( error );
				ExceptionManager.Publish( ce );
				throw ce;
			}

		}

		
		/// <summary>
		/// this overload permits direct passing of the base64-encoded key.  used as a convenience during testing
		/// </summary>
		/// <param name="key">base64-encoded key</param>
		public void Init( string key )
		{
			_key = Convert.FromBase64String( key );
		}

		
		/// <summary>
		/// Helper function to get the base64-encoded key from the "key" node
		/// </summary>
		/// <param name="keyNode">contains the base64 encoded key hash</param>
		/// <returns>byte array which is the key decoded from base64</returns>
		private byte[] ExtractKeyFromNode( XmlNode keyNode )
		{
			byte[] key = null;

			//  skip out right away if passed a null node
			if ( null == keyNode )
			{
				return null;
			}

			try
			{
				//  we're passed the key node, so just take the innerText and convert to a byte[] from base64
				key = Convert.FromBase64String( keyNode.InnerText );
			}
			catch( Exception e )
			{				
				ApplicationUpdateManager.TraceWrite( e, "[KeyValidator.ExtractKeyFromNode]", "RES_CouldNotFindCryptographicKey" );
				ExceptionManager.Publish ( e );
				throw e;
			}

			return key;
			
		}


		/// <summary>
		/// Validate the file given by hashing it using a keyed-hash, and comparing the resulting signature to
		/// the given signature.  Return true if they match.
		/// </summary>
		/// <param name="filePath">full or relative path to file</param>
		/// <param name="signature">the original signature of the file as obtained from the server</param>
		/// <returns>true if signatures match, file is valid</returns>
		bool IValidator.Validate( string filePath, string signature )
		{
			//    HOW THIS WORKS:  
			//  1) load file
			//  2) hash file
			//  3) encrypt given file's hash with the key
			//  4) compare signature with the encrypted hash WE generated from downloaded file;
			//  5) if they match, file is good.
			//  the SECRET is the key.  If both parties hashed the file with the secret key, then the signatures must match.
			//  if the file has been altered or the wrong key was used, they will not match and we must suspect tampering/replacement
			
			byte[] inSignature = null;
			byte[] outSignature = null;

			//  convert base64 signature to byte[]
			inSignature = Convert.FromBase64String( signature );

			//  using the key (member variable) use hashed-key algorithm to generate
			//  a signature for the file
			using ( FileStream fs = new FileStream( filePath, FileMode.Open ) )
			{
				KeyedHashAlgorithm kha = KeyedHashAlgorithm.Create();
				kha.Key = _key;
				outSignature = kha.ComputeHash( fs );
			}

			//  now compare the hash we just created with the signature that was passed.
			//  if they're not identical we can't trust the file
			return compareKeys( outSignature, inSignature );
			
		}
	
		
		/// <summary>
		/// Validates the manifest root xml node against the signature it contains, detects false/tampered manifests
		/// </summary>
		/// <param name="xml">the contents of the manifest</param>
		/// <param name="signature">the encrypted hash code of the contents of the manifest xml file</param>
		/// <returns>true if the decrypted hash signature of the xml matches the hash of the xml generated here</returns>
		bool IValidator.Validate( XmlNode xml, string signature )
		{
			byte[] inSignature = null;
			byte[] outSignature = null;
			byte[] xmlNodeByte = null;

			//  convert xmlnode contents into byte array
			xmlNodeByte = Encoding.Unicode.GetBytes( xml.InnerXml );

			//  convert base64 signature to byte[]
			inSignature = Convert.FromBase64String( signature );

			//  using the key (member variable) use hashed-key algorithm to generate
			//  a signature for the xml
			
			KeyedHashAlgorithm kha = KeyedHashAlgorithm.Create();
			kha.Key = _key;
			outSignature = kha.ComputeHash( xmlNodeByte );
			

			//  now compare the hash we just created with the signature that was passed.
			//  if they're not identical we can't trust the file
			return compareKeys( outSignature, inSignature );
		}

		
		/// <summary>
		/// Reads the given file and creates a hash code that uses every byte of the file to contribute, then encrypts the resulting hash code using the key
		/// in the KeyedHashAlgorithm
		/// </summary>
		/// <param name="filePath">full path to the file to sign</param>
		/// <param name="key">the "secret" key used to "sign" the hash</param>
		/// <returns>a base64-encoded string which is the encrypted signature of the file</returns>
		string IValidator.Sign( string filePath, string key )
		{
			byte[] outSignature = null;
			FileStream fs = null;

			try
			{
				//  using the key (member variable) use hashed-key algorithm to generate
				//  a signature for the file

				//  attempt to open the file with shared read access
				fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.Read );

				//  create an instance of keyed hash algo--note that the static Create is its own Factory method (sweet)
				KeyedHashAlgorithm kha = KeyedHashAlgorithm.Create();
				//  feed the hash algo the key
				kha.Key = Convert.FromBase64String( key );
				//  now hash over entire file, munging with key to give uniqueness that depends on both key and file
				//  for a strong signature.  
				//  NOTE:  now, the "secret" is the key.  Without the "secret", you CANNOT spoof the identity of the file.
				//  the contract between server and client is that the files are what the server promised; the enforcement of that
				//  contract is the cryptographically strong hash, which depends on both key and file identity.
				//  As long as both parties accept the validity of the "secret", they can trust the identity of the file if its signature matches.
				outSignature = kha.ComputeHash( fs );
				
			}
			catch( Exception e )
			{
				ApplicationUpdateManager.TraceWrite( e, "[KeyValidator.Sign]", "RES_EXCEPTION_SigningFile", filePath );
				throw e;
			}
			finally
			{
				if ( null != fs )
				{
					fs.Close();
				}
			}
			//  return the signature
			return Convert.ToBase64String( outSignature );
		}


		/// <summary>
		/// Reads the given xml and creates a hash code that uses every byte of the xml to contribute, then encrypts the resulting hash code using the key
		/// in the KeyedHashAlgorithm
		/// </summary>
		/// <param name="xml">the node of xml to sign</param>
		/// <param name="key">the "secret" key used to "sign" the hash</param>
		/// <returns>a base64-encoded string which is the encrypted signature of the xml</returns>
		string IValidator.Sign( XmlNode xml, string key )
		{
			byte[] outSignature = null;
			byte[] xmlNodeByte = null;

			//  convert xmlnode contents into byte array
			xmlNodeByte = Encoding.Unicode.GetBytes( xml.InnerXml );

			try
			{
				//  create an instance of keyed hash algo--note that the static Create is its own Factory method (sweet)
				KeyedHashAlgorithm kha = KeyedHashAlgorithm.Create();
				//  feed the hash algo the key
				kha.Key = Convert.FromBase64String( key );
				//  key-hash xml
				outSignature = kha.ComputeHash( xmlNodeByte );
			}
			catch( Exception e )
			{
				ApplicationUpdateManager.TraceWrite( e, "[KeyValidator.Sign]", "RES_EXCEPTION_SigningXml" );
				throw e;
			}
			//  no finally, no hard resources used

			//  return key-hash
			return Convert.ToBase64String( outSignature );
		}


		/// <summary>
		/// Compares two keys and return true if both are the same.
		/// </summary>
		/// <param name="firstKey">first key</param>
		/// <param name="secondKey">second key</param>
		/// <returns>true if two keys match byte-for-byte, false otherwise</returns>
		private bool compareKeys( byte[] firstKey, byte[] secondKey )
		{
			if( firstKey.Length != secondKey.Length )
				return false;
			for( int i = 0 ; i < firstKey.Length; i++ )
			{
				if( firstKey[ i ] != secondKey[ i ] )
					return false;
			}
			return true;
		}



		#region IDisposable Implementation

		//  take care of IDisposable too   
		/// <summary>
		/// Allows graceful cleanup of hard resources
		/// </summary>
		public void Dispose() 
		{
			Dispose(true);
			GC.SuppressFinalize(this); 
		}

		/// <summary>
		/// used by externally visible overload.
		/// Does nothing internally, since this validator does not hold onto any hard resources
		/// </summary>
		/// <param name="isDisposing">whether or not to clean up managed + unmanaged/large (true) or just unmanaged(false)</param>
		protected virtual void Dispose(bool isDisposing) 
		{
			if (isDisposing) 
			{
			}
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~KeyValidator()
		{
			// Simply call Dispose(false).
			Dispose (false);
		}

		#endregion

	}
}
