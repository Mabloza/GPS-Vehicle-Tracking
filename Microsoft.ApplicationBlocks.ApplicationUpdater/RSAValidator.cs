
//============================================================================================================
// Microsoft Updater Application Block for .NET
//  http://msdn.microsoft.com/library/en-us/dnbda/html/updater.asp
//	
// RSAValidator.cs
//
// Asymmetric encryption implementation of IValidator, uses RSA to sign/verify files.
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
using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Xml;

using Microsoft.ApplicationBlocks.ApplicationUpdater.Interfaces;

namespace Microsoft.ApplicationBlocks.ApplicationUpdater.Validators
{
	/// <summary>
	/// RSAValidator uses _asymmetric_ (public/private key pair) encryption using RSA, with a SHA1 hash to sign.
	/// This is the stronger security-wise of the two implementations offered by Updater.
	/// </summary>
	public class RSAValidator : IValidator
	{
		// *
		// *
		// **  RSAValidator adapted from sample code from Graeme, thanks Graeme!  **
		// *
		// *

		
		#region Declarations

		private XmlNode							_keyNode				= null;
		private RSAPKCS1SignatureDeformatter	_rsaDeformatter			= null;
		private RSACryptoServiceProvider		_rsaCSP					= null; 
		private const string					HASH_ALGO_SHA1			= "SHA1";

		#endregion

		#region Constructors

		/// <summary>
		/// default constructor.  Real work is done in Init.
		/// </summary>
		public RSAValidator(){}

		#endregion

		#region IValidator Members
		
		/// <summary>
		/// Initialize the Validator with the Public Key.  This is cached after Init.
		/// Also set up the RSA-CSP and the deformatter.
		/// </summary>
		/// <param name="config">xml node from which we expect to extract single node "key" containing RSA public key</param>
		public void Init( XmlNode config )
		{
			try
			{
				//  get the xml node that holds the key 
				_keyNode = config.SelectSingleNode( "key" );

				//  initialize crypto objects for use by Validate overloads later:
				_rsaCSP = new RSACryptoServiceProvider();

				//  load key into CSP
				_rsaCSP.FromXmlString( _keyNode.OuterXml );

				//  create de-formatter
				_rsaDeformatter = new RSAPKCS1SignatureDeformatter( _rsaCSP );

				//  set hash algo to SHA1
				_rsaDeformatter.SetHashAlgorithm( HASH_ALGO_SHA1 );
			}
			catch( Exception e )
			{
				string error = ApplicationUpdateManager.TraceWrite( e, "[RSAValidator.Init]", "RES_CouldNotFindCryptographicKey" );

				throw new ArgumentException( error, "config", e  );
			}
		}


		/// <summary>
		/// Validate given xml node against the cryptographic signature using the PUBLIC key,
		/// return true if the xml is the exactly original value
		/// </summary>
		/// <param name="xml">the node whose _inner xml_ will be validated--so first child is validated</param>
		/// <param name="signature">the original signature, base64 encoded</param>
		/// <returns>false if xml has been changed at all, true if not</returns>
		public bool Validate( XmlNode xml, string signature )
		{ 
			byte[]							nodeForValidating		= null;
			byte[]							hashValue				= null;
			byte[]							signatureByte			= null;
			SHA1Managed						shaHasher				= null; 


			//  load incoming xml node into byte[]
			nodeForValidating = Encoding.Unicode.GetBytes( xml.InnerXml );

			//Create a new instance of SHA1Managed to create the hash value.
			shaHasher = new SHA1Managed();
			//  hash the byte[] from our xml node
			hashValue = shaHasher.ComputeHash( nodeForValidating );

			//  get the signature byte[] from the base64-encoded signature
			signatureByte = Convert.FromBase64String( signature );

			//  finally return result
			return _rsaDeformatter.VerifySignature( hashValue, signatureByte );
		}


		/// <summary>
		/// Validate given FILE against the cryptographic signature using the PUBLIC key
		/// return true if the FILE is the exactly original value
		/// </summary>
		/// <param name="filePath">the path to the file to be validated</param>
		/// <param name="signature">the original signature, base64 encoded</param>
		/// <returns>false if FILE has been changed at all, true if not</returns>
		bool IValidator.Validate( string filePath, string signature )
		{
			FileStream						fs						= null;
			byte[]							hashValue				= null;
			byte[]							signatureByte			= null;
			SHA1Managed						shaHasher				= null; 
			
			//  load stream using given path
			using( fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
			{
				//Create a new instance of SHA1Managed to create the hash value.
				shaHasher = new SHA1Managed();
				//  hash the byte[] from our xml node
				hashValue = shaHasher.ComputeHash( fs );
			}
			
			//  get the signature byte[] from the base64-encoded signature
			signatureByte = Convert.FromBase64String( signature );

			//  finally return result
			return _rsaDeformatter.VerifySignature( hashValue, signatureByte );
		}

		
		/// <summary>
		/// Creates a hash of the INNER xml and cryptographically signs it with the provided PRIVATE key
		/// </summary>
		/// <param name="xml">xml whose InnerXml will be signed</param>
		/// <param name="key">the PRIVATE key of the key pair which must be kept secure</param>
		/// <returns>the base64-encoded signed hash</returns>
		string IValidator.Sign( XmlNode xml, string key )
		{
			byte[]						nodeForValidating	= null;
			byte[]						signedHashValue		= null;
			byte[]						hashValue			= null;
			SHA1Managed					shaHasher			= null;
			RSACryptoServiceProvider	rsaCSP				= null;
			RSAPKCS1SignatureFormatter	rsaFormatter		= null;

			//  grab byte[] from incoming xml node    make sure innerxml encompasses all  
			nodeForValidating = Encoding.Unicode.GetBytes( xml.InnerXml );
			
			shaHasher = new SHA1Managed();
			hashValue = shaHasher.ComputeHash( nodeForValidating );			

			//Generate a public/private key pair from our XML key file.
			rsaCSP = new RSACryptoServiceProvider();

			rsaCSP.FromXmlString( key );

			//Create an RSAPKCS1SignatureFormatter object and pass it the 
			//RSACryptoServiceProvider to transfer the private key.
			rsaFormatter = new RSAPKCS1SignatureFormatter( rsaCSP );

			//Set the hash algorithm to SHA1.
			rsaFormatter.SetHashAlgorithm( HASH_ALGO_SHA1 );

			//Create a signature for hashValue and assign it to 
			//SignedHashValue.
			signedHashValue = rsaFormatter.CreateSignature( hashValue );

			//  return it as base64 encoded
			return Convert.ToBase64String( signedHashValue );
		}

		
		/// <summary>
		/// Creates a hash of the file and cryptographically signs it with the provided PRIVATE key
		/// </summary>
		/// <param name="filePath">path to file for signing</param>
		/// <param name="key">the PRIVATE key of the key pair which must be kept secure</param>
		/// <returns>the base64-encoded signed hash</returns>
		string IValidator.Sign( string filePath, string key )
		{
			FileStream					fs					= null;
			byte[]						signedHashValue		= null;
			byte[]						hashValue			= null;
			SHA1Managed					shaHasher			= null;
			RSACryptoServiceProvider	rsaCSP				= null;
			RSAPKCS1SignatureFormatter	rsaFormatter		= null;

			using( fs = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
			{
				shaHasher = new SHA1Managed();
				hashValue = shaHasher.ComputeHash( fs );
			}

			rsaCSP = new RSACryptoServiceProvider();

			rsaCSP.FromXmlString( key );

			//Create an RSAPKCS1SignatureFormatter object and pass it the 
			//RSACryptoServiceProvider to transfer the private key.
			rsaFormatter = new RSAPKCS1SignatureFormatter( rsaCSP );

			//Set the hash algorithm to SHA1.
			rsaFormatter.SetHashAlgorithm( HASH_ALGO_SHA1 );

			//Create a signature for hashValue and assign it to 
			//SignedHashValue.
			signedHashValue = rsaFormatter.CreateSignature( hashValue );

			//  return it as base64 encoded
			return Convert.ToBase64String( signedHashValue );
		}

		
		#endregion

		#region IDisposable Members

		/// <summary>
		/// Used to clean up resources that might be finalized otherwise.  In this class there are no such resources but 
		/// we must fulfill the IValidator interface contract.
		/// </summary>
		public void Dispose()
		{
			//  nothing to dispose
		}

		
		#endregion
	}
}
