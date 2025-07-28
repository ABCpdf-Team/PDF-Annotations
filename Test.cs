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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

using WebSupergoo.ABCpdf13;
using WebSupergoo.ABCpdf13.Objects;
using WebSupergoo.ABCpdf13.Atoms;
using WebSupergoo.ABCpdf13.Elements;
using WebSupergoo.ABCpdf13.Operations;


namespace WebSupergoo.Annotations {
	public class Test {
		class Server {
			public static string MapPath(string fileName) {
				string theBase = Directory.GetCurrentDirectory();
				return Directory.GetParent(theBase).Parent.FullName + "\\" + fileName;
			}
		}
		public static void Main() {
			const bool warning = false;
			const string msg = "This project will prompt you for a signature. In order to provide the correct signature you will need to first install the JohnSmith.pfx certificate.\r\n\r\n" +
				"You can do this by right clicking on the PFX file and selecting \'Install PFX\'. The password for the certificate is \'1234\'.\r\n\r\n" +
				"Please note that this is a console application and that as such, any notifications or errors will be written to the console window.";
			DialogResult result = (warning) ? MessageBox.Show(msg, "Important Note", MessageBoxButtons.OKCancel) : DialogResult.OK;
			if (result == DialogResult.OK) {
				CreateAnnotationsDemo(Server.MapPath("Annotations.pdf"));
				ModifyFieldsDemo(Server.MapPath("DocToModify.pdf"), Server.MapPath("ModifiedFields.pdf"));
				VerifyFileAndMakeReport(Server.MapPath("Annotations.pdf"), Server.MapPath("VerificationReport.txt"));
				SimpleTextSignature(Server.MapPath("SignedDocument.pdf"));
				if (File.Exists(Server.MapPath("image.png")))
					SimpleImageSignature(Server.MapPath("image.png"), Server.MapPath("SignedDocumentImage.pdf"));
				if ((File.Exists(Server.MapPath("image.png"))) && (File.Exists(Server.MapPath("DocToModify.pdf"))))
					SimpleImageSignatureWithBorder(Server.MapPath("DocToModify.pdf"), Server.MapPath("image.png"), Server.MapPath("SignedDocumentImageBorder.pdf"));
			}
		}

		// <summary>Ask the current user to select a certificate from the his/her
		// personal store.</summary>
		private static X509Certificate2 PromptUserForCert() {
			X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
			store.Open(OpenFlags.ReadOnly);

			try {
				X509Certificate2Collection certs =
					X509Certificate2UI.SelectFromCollection(
					store.Certificates, "Digital Identity",
					"Select a private key to sign",
					X509SelectionFlag.SingleSelection);

				// Return the first cert in the selection
				foreach (X509Certificate2 cert in certs)
					return cert;

				return null;
			}
			finally {
				store.Close();
			}
		}

		// <returns>Friendly name of the cert, using canonical name as fallback.</returns>
		private static string DisplayNameFromCert(X509Certificate2 cert) {
			if (!string.IsNullOrEmpty(cert.FriendlyName))
				return cert.FriendlyName;

			return cert.Subject.Split(',')[0].Split('=')[1];
		}

		public static void CreateAnnotationsDemo(string outputFile) {
			try {
				using (Doc theDoc = new Doc()) {
					theDoc.Font = theDoc.AddFont("Helvetica");
					theDoc.FontSize = 36;

					Catalog cat = theDoc.ObjectSoup.Catalog;

					EmbeddedFileTree fileTree = new EmbeddedFileTree(theDoc);
					fileTree.EmbedFile("MyFile1", Server.MapPath("ABCpdf.swf"), "attachment without annotation");
					theDoc.SetInfo(theDoc.Root, "/PageMode:Name", "UseAttachments");

					theDoc.Pos.X = 40;
					theDoc.Pos.Y = theDoc.MediaBox.Top - 40;
					theDoc.AddText("Interactive Form annotations");

					// Create interactive form
					XForm form = theDoc.Form;
					int fontID = theDoc.AddFont("Times-Roman", LanguageType.Latin);
					string fontName = form.AddResource(theDoc.ObjectSoup[fontID], "Font", "TimesRoman");

					// Radio buttons
					Field radio = form.AddRadioButtonGroup(new XRect[] { new XRect("40 610 80 650"), new XRect("40 660 80 700") }, "RadioGroupField", 0);
					theDoc.Pos.String = "100 696";
					theDoc.AddText("RadioButton 1");
					theDoc.Pos.String = "100 646";
					theDoc.AddText("RadioButton 2");

					// Text fields
					Field text = form.AddTextField(new XRect("40 530 300 580"), "TextField1", "Hello World!");
					FieldElement textE = new FieldElement(text);
					WidgetAnnotationElement textW = new WidgetAnnotationElement(text);
					textE.EntryDA = $"/{fontName} 36 Tf 0 0 1 rg";
					textW.EntryMK = new AppearanceCharacteristicsElement(textE);
					textW.EntryMK.EntryBC = new double[] { 0, 0, 0 };
					textW.EntryMK.EntryBG = new double[] { 220.0 / 255.0, 220.0 / 255.0, 220.0 / 255.0 };
					textE.EntryQ = 0; // Left alignment

					text = form.AddTextField(new XRect("40 460 300 510"), "TextField2", "Text Field");
					textE = new FieldElement(text);
					textW = new WidgetAnnotationElement(text);
					textW.EntryMK = new AppearanceCharacteristicsElement(textE);
					textW.EntryMK.EntryBC = new double[] { 0, 0, 0 };
					textE.EntryDA = $"/{fontName} 36 Tf 0 0 1 rg";
					textE.EntryQ = 0; // Left alignment
					textE.EntryFf |= (int)Field.FieldFlags.Password;

					text = form.AddTextField(new XRect("320 460 370 580"), "TextField3", "Vertical");
					textE = new FieldElement(text);
					textW = new WidgetAnnotationElement(text);
					textW.EntryMK = new AppearanceCharacteristicsElement(textE);
					textW.EntryMK.EntryBC = new double[] { 0, 0, 0 };
					textE.EntryDA = $"/{fontName} 36 Tf 0 0 0 rg";
					textW.EntryMK.EntryR = 90; // Rotation

					// Combobox field
					Field combo = form.AddComboBoxField(new XRect("40 390 300 440"), "ComboBoxField");
					FieldElement comboE = new FieldElement(combo);
					comboE.EntryDA = $"/{fontName} 24 Tf 0 0 0 rg";
					combo.Options = new string[] { "ComboBox Item 1", "ComboBox Item 2", "ComboBox Item 3" };

					// Listbox field
					Field listbox = form.AddListBoxField(new XRect("40 280 300 370"), "ListBoxField");
					FieldElement listboxE = new FieldElement(listbox);
					listboxE.EntryDA = $"/{fontName} 24 Tf 0 0 0 rg";
					listbox.Options = new string[] { "ListBox Item 1", "ListBox Item 2", "ListBox Item 3" };

					// Checkbox field
					form.AddCheckbox(new XRect("40 220 80 260"), "CheckBoxField", true);
					theDoc.Pos.String = "100 256";
					theDoc.AddText("Check Box");

					// Pushbutton field
					Field button = form.AddButton(new XRect("40 160 200 200"), "ButtonField", "Button");
					WidgetAnnotationElement buttonW = new WidgetAnnotationElement(button);
					buttonW.EntryMK = new AppearanceCharacteristicsElement(buttonW);
					buttonW.EntryMK.EntryBC = new double[] { 0, 0, 0 };
					buttonW.EntryBS = new BorderStyleElement(buttonW);
					buttonW.EntryBS.EntryS = "B"; // beveled

					// Markup annotations
					theDoc.Page = theDoc.AddPage();
					theDoc.Pos.X = 40;
					theDoc.Pos.Y = theDoc.MediaBox.Top - 40;
					theDoc.AddText("Markup annotations");

					SquareAnnotation square = new SquareAnnotation(theDoc, new XRect("40 560 300 670"), XColor.FromRgb(255, 0, 0), XColor.FromRgb(0, 0, 255));
					square.SquareElement.EntryBS = new BorderStyleElement(square.SquareElement);
					square.SquareElement.EntryBS.EntryW = 8;

					LineAnnotation line = new LineAnnotation(theDoc, new XPoint("100 565"), new XPoint("220 665"), XColor.FromRgb(255, 0, 0));
					line.LineElement.EntryBS = new BorderStyleElement(line.LineElement);
					line.LineElement.EntryBS.EntryW = 12;
					line.RichTextCaption = "<span style= \"font-size:36pt; color:#FF0000\">Line</span>";

					theDoc.FontSize = 24;
					theDoc.Pos.String = "400 670";
					int id = theDoc.AddText("Underline");
					TextMarkupAnnotation markup = new TextMarkupAnnotation(theDoc, fontID, TextMarkupType.Underline, XColor.FromRgb(0, 255, 0));

					theDoc.Pos.String = "400 640";
					fontID = theDoc.AddText("Highlight");
					markup = new TextMarkupAnnotation(theDoc, fontID, TextMarkupType.Highlight, XColor.FromRgb(255, 255, 0));

					theDoc.Pos.String = "400 610";
					fontID = theDoc.AddText("StrikeOut");
					markup = new TextMarkupAnnotation(theDoc, fontID, TextMarkupType.StrikeOut, XColor.FromRgb(255, 0, 0));

					theDoc.Pos.String = "400 580";
					fontID = theDoc.AddText("Squiggly");
					markup = new TextMarkupAnnotation(theDoc, fontID, TextMarkupType.Squiggly, XColor.FromRgb(0, 0, 255));

					CircleAnnotation circle = new CircleAnnotation(theDoc, new XRect("80 320 285 525"), XColor.FromRgb(255, 255, 0), XColor.FromRgb(255, 128, 0));
					circle.CircleElement.EntryBS = new BorderStyleElement(circle.CircleElement);
					circle.CircleElement.EntryBS.EntryW = 20;
					circle.CircleElement.EntryBS.EntryS = "D"; // dashed
					circle.CircleElement.EntryBS.EntryD = new ArrayElement<Element>(Atom.FromString("[3 2]"), cat);

					LineAnnotation arrowLine = new LineAnnotation(theDoc, new XPoint("385 330"), new XPoint("540 520"), XColor.FromRgb(255, 0, 0));
					arrowLine.LineEndingsStyle = "ClosedArrow ClosedArrow";
					arrowLine.LineElement.EntryBS = new BorderStyleElement(arrowLine.LineElement);
					arrowLine.LineElement.EntryBS.EntryW = 6;
					arrowLine.FillColor = XColor.FromRgb(255, 0, 0);

					double[] v1 = new double[] { 100, 70, 50, 120, 50, 220, 100, 270, 200, 270, 250, 220, 250, 120, 200, 70 };
					PolygonAnnotation polygon = new PolygonAnnotation(theDoc, v1, XColor.FromRgb(255, 0, 0), XColor.FromRgb(0, 255, 0));
					double[] v2 = new double[] { 400, 70, 350, 120, 350, 220, 400, 270, 500, 270, 550, 220, 550, 120, 500, 70 };
					PolygonAnnotation cloudyPolygon = new PolygonAnnotation(theDoc, v2, XColor.FromRgb(255, 0, 0), XColor.FromRgb(64, 85, 255));
					cloudyPolygon.CloudyEffect = 1;

					// Movie annotations
					// WMV is courtesy of NASA - http://www.nasa.gov/wmv/30873main_cardiovascular_300.wmv
					theDoc.Page = theDoc.AddPage();
					theDoc.Pos.X = 40;
					theDoc.Pos.Y = theDoc.MediaBox.Top - 40;
					theDoc.AddText("Multimedia features");

					theDoc.FontSize = 24;

					// Adobe says that Flash Player is not supported after December 2020 so this content addition is disabled.
					// Of course "not supported" does not mean "does not work" which is why the code has been left here.
					//theDoc.Pos.String = "40 690";
					//theDoc.AddText("Flash movie:");
					//ScreenAnnotation movie1 = new ScreenAnnotation(theDoc, new XRect("40 420 300 650"), Server.MapPath("ABCpdf.swf"));

					//theDoc.Pos.String = "312 690";
					//theDoc.AddText("Flash rich media:");
					//RichMediaAnnotation media1 = new RichMediaAnnotation(theDoc, new XRect("312 420 572 650"), Server.MapPath("ABCpdf.swf"), "Flash");

					theDoc.Pos.String = "40 400";
					theDoc.AddText("Video File:");
					ScreenAnnotation movie2 = new ScreenAnnotation(theDoc, new XRect("80 40 520 360"), Server.MapPath("video.wmv"));

					theDoc.Page = theDoc.AddPage();
					theDoc.FontSize = 36;
					theDoc.Pos.X = 40;
					theDoc.Pos.Y = theDoc.MediaBox.Top - 40;
					theDoc.AddText("Other types of annotations");

					// Sticky note annotation
					theDoc.FontSize = 24;
					theDoc.Pos.String = "40 680";
					theDoc.AddText("Text annotation");
					TextAnnotation textAnnotation = new TextAnnotation(theDoc, new XRect("340 660 360 680"), new XRect("550 650 600 750"), "6 sets of 13 pages. Trim to 5X7.");

					// File attachment annotation
					theDoc.Pos.String = "40 640";
					theDoc.AddText("File Attachment annotation");
					FileAttachmentAnnotation fileAttachment = new FileAttachmentAnnotation(theDoc, new XRect("340 620 360 640"), Server.MapPath("video.WMV"));

					// StampAnnotations
					theDoc.Pos.String = "40 600";
					theDoc.AddText("Stamp annotations");
					StampAnnotation stamp1 = new StampAnnotation(theDoc, new XRect("340 560 540 600"), "DRAFT", XColor.FromRgb(0, 0, 128));
					StampAnnotation stamp2 = new StampAnnotation(theDoc, new XRect("340 505 540 545"), "FINAL", XColor.FromRgb(0, 128, 0));
					StampAnnotation stamp3 = new StampAnnotation(theDoc, new XRect("340 450 540 490"), "NOT APPROVED", XColor.FromRgb(128, 0, 0));

					string obj = Path.Combine(Path.GetTempPath(), "sphere.obj");
					string mtl = Path.Combine(Path.GetTempPath(), "sphere.mtl");
					try {
						File.Copy(Server.MapPath("sphere.obj.txt"), obj, true);
						File.Copy(Server.MapPath("sphere.mtl.txt"), mtl, true);
						D3DAnnotation d3d = new D3DAnnotation(theDoc, new XRect("40 40 440 440"));
						d3d.SetOBJ(obj);
					}
					finally {
						File.Delete(obj);
						File.Delete(mtl);
					}

					theDoc.PageNumber = 1;

					// Link annotations
					theDoc.Rect.SetSides(450, 530, 550, 580);
					theDoc.Color.String = "128 128 255";
					theDoc.FillRect();
					var link = new LinkAnnotation(theDoc, theDoc.Rect);
					var dest = new DestinationElement(link.LinkElement);
					var page2 = theDoc.ObjectSoup.Catalog.Pages.GetPageArrayAll()[1];
					dest.SetTarget(new PageObjectElement(page2), DestinationType.Fit);
					link.LinkElement.EntryDest = dest;
					theDoc.Color.String = "0 0 0";

					// Some releases of Acrobat are problematic. eg 2022.003.20322
					// They have things they want to add to Annotations, for example
					// an NM property. These properties are optional and indeed
					// somewhat niche, so they are not generally added by other
					// software.
					//
					// However if, after signing, you click on the 'Validate' button
					// on the 'Signature Panel' Acrobat will complain that the changes
					// it has made have resulted in a change to the document. So it
					// becomes a problem.
					//
					// Presumably at some point Adobe will see this as a bug in Acrobat
					// and fix it, but in the short run it is probably best to cater to
					// these ideosyncracies by adding in the bits Acrobat wants.
					Page[] pages = theDoc.ObjectSoup.Catalog.Pages.GetPageArrayAll();
					foreach (Page page in pages) {
						foreach (var annot in page.GetAnnotations()) {
							var ae = new AnnotationElement(annot);
							if (ae.EntryAP == null || ae.EntryAP.EntryN == null)
								annot.UpdateAppearance();
							if (ae.EntryNM == null)
								ae.EntryNM = Guid.NewGuid().ToString();
							if (ae.EntrySubtype == "Text") {
								var e = new TextAnnotationElement(annot);
								e.EntryF |= (int)(Annotation.AnnotationFlags.NoZoom | Annotation.AnnotationFlags.NoRotate);
							}
						}
					}

					// Signature fields
					// Add signature fields last so that entire document is signed

					// Note: For maximum portability, it is recommended
					// that you create all the signature fields before 
					// signing any one of them (as is demonstrated below).
					//
					// The reason is that adding a signature field 
					// changes the document. Although this is legal, not all 
					// versions of Acrobat are happy with all types of updates.
					// As such, they may report existing valid signatures to be 
					// invalid.
					//
					// In the past, the "SigFlags" entry allowed contents to be 
					// appended to signed documents using incremental updates.
					// However, the entry's treatment seems to have changed in 
					// Adobe Reader X. For example, Adobe Reader X will 
					// report the signatures in `SignedThenAppended.pdf` to 
					// be invalid whereas Acrobat 8 will report 
					// "signatures are valid, but document has been changed 
					// since it was signed."
					// This `SignedThenAppended.pdf` file is a document created
					// using Acrobat Professional 8. So given that Adobe products
					// are not internally consistent here, it is difficult to seee
					// how any other products can be.

					// We add the first signature field unsigned
					Signature sig1 = form.AddSignature(new XRect("40 100 240 150"), "Signature1");

					if (true) {
						// And the second signature field unsigned.
						// We will then add and sign a third signature.
						// Then we will go back and sign the second signature.
						Signature sig2 = form.AddSignature(new XRect("340 100 540 150"), "Signature2");
						X509Certificate2 userCert = PromptUserForCert();
						if (userCert != null) {
							sig2.Signer = DisplayNameFromCert(userCert);
							// We need not call sig2.Commit() as sig2 is not signed yet.
						}

						// We can add the last signature field signed and with appearance.
						Signature sig3 = form.AddSignature(new XRect("340 160 540 220"), "Signature3");
						sig3.Sign(Server.MapPath("JohnSmith.pfx"), "1234");
						sig3.Reason = "I am the author";
						sig3.Location = "New York";
						VirtualPageOperation op = new VirtualPageOperation(theDoc, sig3.Rect.Width, sig3.Rect.Height);
						theDoc.FitText($"Digitally signed by {sig3.Signer}\nReason: {sig3.Reason}\nLocation: {sig3.Location}\nDate: {sig3.SigningUtcTime:yyyy.MM.dd}");
						sig3.GetAnnotations()[0].NormalAppearance = op.GetFormXObject();
						theDoc.Page = sig3.PageID;
						sig3.Commit();

						// Go back and sign the second signature placeholder
						// if interactive user has supplied the signing key
						if (userCert != null) {
							sig2 = (Signature)theDoc.Form.Fields["Signature2"];
							sig2.Sign(userCert, true);
						}
					}

					theDoc.SaveOptions.Linearize = false;
					theDoc.SaveOptions.Remap = false;
					theDoc.Save(outputFile);
				}
			}
			catch (Exception e) {
				Console.WriteLine("An exception occurred during document signing:");
				Console.WriteLine("Message: " + e.Message);
				Console.WriteLine("Source: " + e.Source);
			}
		}

		public static void ModifyFieldsDemo(string inputFile, string outputFile) {
			if (!File.Exists(inputFile))
				return;
			try {
				using (Doc theDoc = new Doc()) {

					theDoc.Read(inputFile);

					theDoc.Font = theDoc.AddFont("Helvetica");
					theDoc.FontSize = 36;

					// Create interactive form
					XForm form = theDoc.Form;
					int fontID = theDoc.AddFont("Times-Roman", LanguageType.Latin);
					string fontName = form.AddResource(theDoc.ObjectSoup[fontID], "Font", "TimesRoman");

					// Add Radio buttons
					form.AddRadioButtonGroup(new XRect[] { new XRect("40 610 80 650"), new XRect("40 660 80 700") }, "RadioGroupField", 0);
					theDoc.Pos.String = "100 696";
					theDoc.AddText("RadioButton 1");
					theDoc.Pos.String = "100 646";
					theDoc.AddText("RadioButton 2");

					// Fields cannot have the same name. If we are going to add duplicate field name then later 
					// we will need to rationalize the document structure so that the two fields will synchronize.
					string textFieldName = "TextField1";
					bool fieldAlreadyExists = theDoc.Form[textFieldName] != null;

					// Add Text field
					Field text = form.AddTextField(new XRect("40 530 300 580"), textFieldName, "Hello World!");
					FieldElement textE = new FieldElement(text);
					WidgetAnnotationElement textW = new WidgetAnnotationElement(text);
					textE.EntryDA = $"/{fontName} 36 Tf 0 0 1 rg";
					textW.EntryMK = new AppearanceCharacteristicsElement(textE);
					textW.EntryMK.EntryBC = new double[] { 0, 0, 0 };
					textW.EntryMK.EntryBG = new double[] { 220.0 / 255.0, 220.0 / 255.0, 220.0 / 255.0 };
					textE.EntryQ = 0; // Left alignment

					// Here we need rationalize if necessary
					if (fieldAlreadyExists)
						form.MakeFieldIntoGroup(text);

					// Add Synchronized Text Fields
					List<IndirectObject> kids = new List<IndirectObject>();
					kids.Add(form.AddTextField(new XRect("40 230 300 280"), null, null));
					kids.Add(form.AddTextField(new XRect("40 170 300 220"), null, null));
					form.AddGroupField(kids, "Group Field", "Synchronized");

					// Delete Pre-Existing Fields (ones that appear to relate to text)
					string[] names = theDoc.Form.GetFieldNames();
					foreach (string name in names) {
						if (name.Contains("Text")) {
							Field field = theDoc.Form[name];
							form.Remove(field);
						}
					}

					// Move Pre-Existing Fields (ones that appear to relate to names)
					foreach (string name in names) {
						if (name.Contains("Name")) {
							Field field = theDoc.Form[name];
							Annotation[] annots = field.GetAnnotations();
							foreach (Annotation annot in annots) {
								XRect rect = annot.Rect;
								rect.Move(0, -200); // move down
								rect.Magnify(2, 2); // amd make bigger
								annot.Rect = rect;
							}
						}
					}

#if DEBUG
					// these options make PDF files more diff-able
					theDoc.SaveOptions.Linearize = false;
					theDoc.SaveOptions.Remap = false;
#endif
					theDoc.Save(outputFile);
				}
			}
			catch (Exception e) {
				Console.WriteLine("An exception occurred:");
				Console.WriteLine("Message: " + e.Message);
				Console.WriteLine("Source: " + e.Source);
			}
		}

		public static void VerifyFileAndMakeReport(string fileName, string reportFileName) {
			try {
				StringBuilder sb = new StringBuilder();
				sb.AppendLine("Signature verification report");
				sb.AppendLine("File name: " + fileName);

				using (Doc subjectDoc = new Doc()) {
					subjectDoc.Read(fileName);

					// certificates are used for validating the X.509 signature
					// you may wish to obtain root certificates from a trusted authority
					string[] certs = Server.MapPath("JohnSmith.cer").Split(new char[] { ';' });

					int sigCount = 0;
					foreach (Field theField in subjectDoc.Form.Fields) {
						if (theField is Signature) {
							sigCount++;
							sb.AppendLine("");
							Signature theSig = (Signature)theField;
							string reportHtml = null;
							if (theSig.Signer != null && theSig.Signer.Length > 0) {
								bool certificateValid = theSig.Validate(certs);
								IndirectObject v = theSig.ResolveObj(Atom.GetItem(theSig.Atom, "V"));
								int revision = v != null ? v.Revision : theSig.Revision;
								reportHtml = "Signature name: " + theSig.Name + "\r\n" +
									"Signed by: " + theSig.Signer + "\r\n" +
									"Reason: " + theSig.Reason + "\r\n" +
									"Date (UTC Time): " + theSig.SigningUtcTime + "\r\n" +
									"Location: " + theSig.Location + "\r\n" +
									"Is document modified: " + theSig.IsModified.ToString() + "\r\n" + // document has been modified or tampered with in some way - implies signature not valid
									"Is certificate valid: " + certificateValid.ToString() + "\r\n" +
									"Is document valid: " + (certificateValid && !theSig.IsModified).ToString() + "\r\n" +
									"Has document been updated: " + (revision < subjectDoc.ObjectSoup.Revisions).ToString() + "\r\n"; // signature applies to a previous revision of the document - valid but changes have been made
							}
							else {
								reportHtml = "Signature name: " + theSig.Name + "\r\n" +
									"This signature has not been signed.\r\n";
							}
							sb.Append(reportHtml);

							foreach (byte[] certData in theSig.GetCertificates()) {
								X509Certificate2 theCert = new X509Certificate2(certData);
								sb.Append(AddCertificateDetails(theCert).ToString());
							}
						}
					}

					if (sigCount == 0)
						sb.AppendLine("The document is not signed.");
				}

				File.WriteAllText(reportFileName, sb.ToString());
			}
			catch (Exception e) {
				Console.WriteLine("An exception occured during document validation:");
				Console.WriteLine("Message: " + e.Message);
				Console.WriteLine("Source: " + e.Source);
			}
		}

		private static StringBuilder AddCertificateDetails(X509Certificate2 inCert) {

			StringBuilder sb = new StringBuilder();
			sb.Append("Certificate details:" + "\r\n");
			sb.Append("Subject: " + inCert.Subject + "\r\n");
			sb.Append("Issued by: " + inCert.IssuerName.Name + "\r\n");
			sb.Append("From: " + inCert.NotBefore.ToString() + " To: " + inCert.NotAfter.ToString() + "\r\n");
			sb.Append("Serial Number:" + inCert.GetSerialNumberString() + "\r\n");
			sb.Append("Version: " + inCert.Version.ToString() + "\r\n");
			sb.Append("Algorithm: " + inCert.SignatureAlgorithm.FriendlyName + "\r\n");
			foreach (X509Extension e in inCert.Extensions)
				sb.Append(e.Oid.FriendlyName + ": " + e.Format(false) + "\r\n");
			sb.Append("Public Key: " + inCert.PublicKey.Key.KeyExchangeAlgorithm + "\r\n");
			sb.Append("Public Key Data " + inCert.GetPublicKeyString() + "\r\n");
			return sb;
		}

		public static void SimpleTextSignature(string outputFile) {
			try {
				using (Doc theDoc = new Doc()) {
					theDoc.Font = theDoc.AddFont("Helvetica");
					theDoc.FontSize = 36;

					// Create interactive form
					XForm form = theDoc.Form;
					theDoc.Pos.X = 40;
					theDoc.Pos.Y = theDoc.MediaBox.Top - 40;
					theDoc.AddText("Simple signed document");

					// add signature
					Signature sig = form.AddSignature(new XRect("340 160 540 220"), "Signature");
					sig.LockFields();
					sig.Sign(Server.MapPath("JohnSmith.pfx"), "1234");
					sig.Reason = "I am the author";
					sig.Location = "New York";

					// Certifying the document makes all but the last signed signature invalid.
					// See the CertifyDocument function for details.
					sig.Certify();

					// make signature appearance
					VirtualPageOperation op = new VirtualPageOperation(theDoc, sig.Rect.Width, sig.Rect.Height);
					theDoc.FitText($"Digitally signed by {sig.Signer}\nReason: {sig.Reason}\nLocation: {sig.Location}\nDate: {sig.SigningUtcTime:yyyy.MM.dd}");
					sig.GetAnnotations()[0].NormalAppearance = op.GetFormXObject();

#if DEBUG
					// these options make PDF files more diff-able
					theDoc.SaveOptions.Linearize = false;
					theDoc.SaveOptions.Remap = false;
#endif
					theDoc.Save(outputFile);
				}
			}
			catch (Exception e) {
				Console.WriteLine("An exception occurred during document signing:");
				Console.WriteLine("Message: " + e.Message);
				Console.WriteLine("Source: " + e.Source);
			}
		}

		public static void SimpleImageSignature(string imageFile, string outputFile) {
			try {
				using (Doc theDoc = new Doc()) {
					theDoc.Font = theDoc.AddFont("Helvetica");
					theDoc.FontSize = 36;

					// Create interactive form
					XForm form = theDoc.Form;
					theDoc.Pos.X = 40;
					theDoc.Pos.Y = theDoc.MediaBox.Top - 40;
					theDoc.AddText("Simple signed document");

					// add signature
					// Certifying the document makes all but the last signed signature invalid.
					// See the CertifyDocument function for details.
					Signature sig = form.AddSignature(new XRect("340 160 540 220"), "Signature");
					sig.LockFields();
					sig.Sign(Server.MapPath("JohnSmith.pfx"), "1234");
					sig.Certify();

					// make signature appearance
					VirtualPageOperation op = new VirtualPageOperation(theDoc, sig.Rect.Width, sig.Rect.Height);
					using (XImage theImage = XImage.FromFile(imageFile, null))
						theDoc.AddImage(theImage);
					sig.GetAnnotations()[0].NormalAppearance = op.GetFormXObject();

#if DEBUG
					// these options make PDF files more diff-able
					theDoc.SaveOptions.Linearize = false;
					theDoc.SaveOptions.Remap = false;
#endif
					theDoc.Save(outputFile);
				}
			}
			catch (Exception e) {
				Console.WriteLine("An exception occurred during document signing:");
				Console.WriteLine("Message: " + e.Message);
				Console.WriteLine("Source: " + e.Source);
			}
		}

		public static void SimpleImageSignatureWithBorder(string originalPdf, string imageFile, string outputFile) {
			try {
				using (Doc theDoc = new Doc()) {
					theDoc.Read(originalPdf);
					theDoc.Font = theDoc.AddFont("Helvetica");
					theDoc.FontSize = 36;

					// Create interactive form
					XForm form = theDoc.Form;
					theDoc.Rect.Inset(40, 40);
					theDoc.AddText("Document Read\r\nAdd Unsigned Signature\r\nShow Image & Border");

					// add signature
					Signature sig = form.AddSignature(new XRect("340 160 540 220"), "Signature");

					// make signature appearance
					VirtualPageOperation op = new VirtualPageOperation(theDoc, sig.Rect.Width, sig.Rect.Height);
					using (XImage theImage = XImage.FromFile(imageFile, null))
						theDoc.AddImage(theImage);
					theDoc.Color.SetRgb(255, 0, 0);
					theDoc.FrameRect(true);
					sig.GetAnnotations()[0].NormalAppearance = op.GetFormXObject();

#if DEBUG
					// these options make PDF files more diff-able
					theDoc.SaveOptions.Linearize = false;
					theDoc.SaveOptions.Remap = false;
#endif
					theDoc.Save(outputFile);
				}
			}
			catch (Exception e) {
				Console.WriteLine("An exception occurred during document signing:");
				Console.WriteLine("Message: " + e.Message);
				Console.WriteLine("Source: " + e.Source);
			}
		}
	}
}

