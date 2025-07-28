// ===========================================================================
//	©2013-2024 WebSupergoo. All rights reserved.
//
//	This source code is for use exclusively with the ABCpdf product with
//	which it is distributed, under the terms of the license for that
//	product. Details can be found at
//
//		http://www.websupergoo.com/
//
//	This copyright notice must not be deleted and must be reproduced alongside
//	any sections of code extracted from this module.
// ===========================================================================

using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;

using WebSupergoo.ABCpdf13;
using WebSupergoo.ABCpdf13.Objects;
using WebSupergoo.ABCpdf13.Atoms;
using WebSupergoo.ABCpdf13.Elements;


namespace WebSupergoo.Annotations {
	public class TreeUtilities {
		/// <summary>Embeds a file and adds it to the name tree.</summary>
		/// <param name="key">A key that uniquely identifies an embedded file in the name tree.</param>
		/// <param name="filePath">A file path.</param>
		/// <param name="description">A description.</param>
		public static FullFileSpecificationElement EmbedFile(string filePath, string description, ObjectSoup soup) {
			FileInfo info = new FileInfo(filePath);
			EmbeddedFileStreamElement stream = EmbedFileStream(info, soup);
			CatalogElement cat = new CatalogElement(soup.Catalog);
			FullFileSpecificationElement file = new FullFileSpecificationElement(cat);
			file.EntryType = "Filespec";
			file.EntryF = info.Name;
			file.EntryUF = info.Name;
			file.EntryEF = new EmbeddedFileSetElement(file);
			file.EntryEF.EntryF = stream;
			file.EntryDesc = description;
			return file;
		}

		/// <summary>Embeds a file.</summary>
		/// <param name="info">A file info.</param>
		/// <returns>The ID of the embedded file stream.</returns>
		public static EmbeddedFileStreamElement EmbedFileStream(FileInfo info, ObjectSoup soup) {
			string filePath = info.FullName;
			StreamObject obj = new StreamObject(soup, filePath);
			if (obj.Length > 64)
				obj.CompressFlate();
			EmbeddedFileStreamElement fs = new EmbeddedFileStreamElement(obj);
			fs.EntryType = "EmbeddedFile";
			fs.EntrySubtype = GetContentType(filePath);
			fs.EntryParams = new EmbeddedFileParameterElement(fs);
			fs.EntryParams.EntrySize = (int)info.Length;
			fs.EntryParams.EntryModDate = StringAtom.DateToString(info.LastWriteTimeUtc);
			fs.EntryParams.EntryCreationDate = StringAtom.DateToString(info.CreationTimeUtc);
			return fs;
		}

		/// <summary>Gets the content type of a file.</summary>
		/// <param name="fileName">A file name or path.</param>
		/// <returns>The content type.</returns>
		protected static string GetContentType(string fileName) {
			if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
				return "application/pdf";
			if (fileName.EndsWith(".swf", StringComparison.OrdinalIgnoreCase))
				return "application/x-shockwave-flash";
			if (fileName.EndsWith(".wmv", StringComparison.OrdinalIgnoreCase))
				return "video/x-ms-wmv";
			if (fileName.EndsWith(".mpg", StringComparison.OrdinalIgnoreCase))
				return "video/mpeg";
			if (fileName.EndsWith(".avi", StringComparison.OrdinalIgnoreCase))
				return "video/avi";
			return null;
		}
	}

	/// <summary>The name tree for embedded file streams.</summary>
	public class EmbeddedFileTree : NameOrNumberTree<string, FileSpecificationElement> {
		/// <summary>EmbeddedFileTree constructor.</summary>
		/// <param name="doc">A document.</param>
		public EmbeddedFileTree(Doc doc) {
			CatalogElement cat = new CatalogElement(doc.ObjectSoup.Catalog);
			if (cat.EntryNames == null)
				cat.EntryNames = new NameDictionaryElement(cat);
			if (cat.EntryNames.EntryEmbeddedFiles == null) {
				cat.EntryNames.EntryEmbeddedFiles = new NameTreeNodeElement<FileSpecificationElement>(cat);
				cat.EntryNames.EntryEmbeddedFiles.EntryNames = new ArrayElement<Element>(cat);
			}
			Root = cat.EntryNames.EntryEmbeddedFiles;
		}

		/// <summary>Embeds a file and adds it to the name tree.</summary>
		/// <param name="key">A key that uniquely identifies an embedded file in the name tree.</param>
		/// <param name="filePath">A file path.</param>
		/// <param name="description">A description.</param>
		public void EmbedFile(string key, string filePath, string description) {
			FullFileSpecificationElement file = TreeUtilities.EmbedFile(filePath, description, Root.Host.Soup);
			Add(key, file);
		}
	}

	/// <summary>The name tree for document-level JavaScript actions.</summary>
	public class JavaScriptTree : NameOrNumberTree<string, JavaScriptActionElement> {
		/// <summary>JavaScriptTree constructor.</summary>
		/// <param name="doc">A document.</param>
		public JavaScriptTree(Doc doc)  {
			CatalogElement cat = new CatalogElement(doc.ObjectSoup.Catalog);
			if (cat.EntryNames == null)
				cat.EntryNames = new NameDictionaryElement(cat);
			if (cat.EntryNames.EntryJavaScript == null) {
				cat.EntryNames.EntryJavaScript = new NameTreeNodeElement<JavaScriptActionElement>(cat);
				cat.EntryNames.EntryJavaScript.EntryNames = new ArrayElement<Element>(cat);
			}
			Root = cat.EntryNames.EntryJavaScript;
		}

		/// <summary>Adds a JavaScript action to the name tree.</summary>
		/// <param name="key">A key that uniquely identifies a JavaScript action in the name tree.</param>
		/// <param name="script">JavaScript code.</param>
		public void AddScript(string key, string script) {
			AddScript(key, script, script.Length > 128);
		}

		/// <summary>Adds a JavaScript action to the name tree.</summary>
		/// <param name="key">A key that uniquely identifies a JavaScript action in the name tree.</param>
		/// <param name="script">JavaScript code.</param>
		/// <param name="useStream">Whether to use a stream (otherwise, a string) to store JavaScript code.</param>
		public void AddScript(string key, string script, bool useStream) {
			JavaScriptActionElement action = new JavaScriptActionElement(Root);
			action.EntryType = "Action";
			action.EntryS = "JavaScript";
			if (!useStream)
				action.EntryJS = new Element(new StringAtom(script), action.Host);
			else {
				StreamObject stream = new StreamObject(action.Host.Soup);
				stream.SetText(script);
				if (stream.Length > 64)
					stream.CompressFlate();
				action.EntryJS = new Element(stream);
			}
			Add(key, action);
		}
	}
}
