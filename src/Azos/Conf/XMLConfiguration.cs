/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Text;
using System.Xml;
using System.IO;

namespace Azos.Conf
{
  /// <summary>
  /// Provides implementation of configuration based on a classic XML content
  /// </summary>
  [Serializable]
  public class XMLConfiguration : FileConfiguration
  {
    #region .ctor / static

    /// <summary>
    /// Creates an instance of a new configuration not bound to any XML file
    /// </summary>
    public XMLConfiguration() : base() { }

    /// <summary>
    /// Creates an instance of the new configuration and reads contents from an XML file
    /// </summary>
    public XMLConfiguration(string filename) : base(filename)
    {
      readFromFile();
    }

    /// <summary>
    /// Creates an instance of configuration initialized from XML content passed as string
    /// </summary>
    public static XMLConfiguration CreateFromXML(string content, bool strictNames = true)
    {
      var result = new XMLConfiguration();
      result.StrictNames = strictNames;
      result.readFromString(content);

      return result;
    }

    #endregion


    #region Public Properties

    #endregion


    #region Public

    /// <summary>
    /// Saves configuration into a file
    /// </summary>
    public override void SaveAs(string filename)
    {
      SaveAs(filename, null);

      base.SaveAs(filename);
    }

    /// <summary>
    /// Saves configuration to a file with optional link to XSL file
    /// </summary>
    public void SaveAs(string filename, string xsl, string encoding = null)
    {
      if (string.IsNullOrEmpty(filename))
        throw new ConfigException(StringConsts.CONFIGURATION_FILE_UNKNOWN_ERROR);

      var doc = buildXmlDoc(xsl, encoding);
      doc.Save(filename);
    }

    /// <summary>
    /// Saves configuration to a TextWriter with optional link to XSL file
    /// </summary>
    public void SaveAs(TextWriter wri, string xsl = null, string encoding = null)
    {
      var doc = buildXmlDoc(xsl, encoding);
      doc.Save(wri.NonDisposed(nameof(wri)));
    }

    /// <summary>
    /// Saves configuration to a TextWriter with optional link to XSL file
    /// </summary>
    public void SaveAs(XmlWriter wri, string xsl = null, string encoding = null)
    {
      var doc = buildXmlDoc(xsl, encoding);
      doc.Save(wri.NonDisposed(nameof(wri)));
    }

    /// <summary>
    /// Saves XML configuration with optional link to XSL file, into string and returns it
    /// </summary>
    public string SaveToString(string xsl = null, string encoding = null)
    {
      var doc = buildXmlDoc(xsl, encoding);
      using (var writer = new StringWriter())
      {
        doc.Save(writer);
        return writer.ToString();
      }
    }

    /// <summary>
    /// Saves XML configuration into stream
    /// </summary>
    public XmlDocument SaveToXmlDoc(string xsl = null, string encoding = null)
    {
      return buildXmlDoc(xsl, encoding);
    }

    /// <inheritdoc/>
    public override void Refresh() => readFromFile();

    /// <inheritdoc/>
    public override void Save()
    {
      SaveAs(m_FileName);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
      XmlDocument doc = new XmlDocument();

      buildDocNode(doc, null, m_Root);

      return doc.OuterXml;
    }

    #endregion


    #region Protected

    //for XML we only allow printable chars and 0..9 and - or _ .
    protected override string AdjustNodeName(string name)
    {
      var result = new StringBuilder(32);//average id size is 16-20 chars

      foreach (var c in name)
        if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
          result.Append(c);
        else
          result.Append('-');

      return result.ToString();
    }

    #endregion


    #region Private Utils

    private void readFromFile()
    {
      XmlDocument doc = new XmlDocument();

      doc.Load(m_FileName);

      read(doc);
    }

    private void readFromString(string content)
    {
      XmlDocument doc = new XmlDocument();

      doc.LoadXml(content);

      read(doc);
    }

    private void read(XmlDocument doc)
    {
      m_Root = buildNode(doc.DocumentElement, null);
      if (m_Root != null)
        m_Root.ResetModified();
      else
        m_Root = m_EmptySectionNode;
    }

    private ConfigSectionNode buildNode(XmlNode xnode, ConfigSectionNode parent)
    {
      ConfigSectionNode result;

      if (xnode.NodeType == XmlNodeType.Text && parent != null)
      {
        parent.Value = xnode.Value;
        return null;
      }

      if (parent != null)
        result = parent.AddChildNode(xnode.Name, string.Empty);
      else
        result = new ConfigSectionNode(this, null, xnode.Name, string.Empty);

      if (xnode.Attributes != null)
        foreach (XmlAttribute xattr in xnode.Attributes)
          result.AddAttributeNode(xattr.Name, xattr.Value);


      foreach (XmlNode xn in xnode)
        if (xn.NodeType != XmlNodeType.Comment)
          buildNode(xn, result);

      return result;
    }

    private XmlDocument buildXmlDoc(string xsl, string encoding = null)
    {
      return BuildXmlDocFromRoot(m_Root, xsl, encoding);
    }

    internal static XmlDocument BuildXmlDocFromRoot(ConfigSectionNode root, string xsl, string encoding = null)
    {
      var doc = new XmlDocument();

      //insert XSL link
      if (!string.IsNullOrEmpty(xsl))
      {
        var decl = doc.CreateXmlDeclaration("1.0", encoding, null);
        doc.AppendChild(decl);
        var link = doc.CreateProcessingInstruction(
                           "xml-stylesheet",
                           "type=\"text/xsl\" href=\"" + xsl + "\"");
        doc.AppendChild(link);
      }

      if (encoding.IsNotNullOrWhiteSpace())
      {
        var decl = doc.CreateXmlDeclaration("1.0", encoding, null);
        doc.AppendChild(decl);
      }

      buildDocNode(doc, null, root);

      return doc;
    }

    private static void buildDocNode(XmlDocument doc, XmlNode xnode, ConfigSectionNode node)
    {
      XmlNode xnew = doc.CreateElement(node.Name);

      if (xnode != null)
        xnode.AppendChild(xnew);
      else
        doc.AppendChild(xnew);

      foreach (ConfigAttrNode anode in node.Attributes)
      {
        XmlNode xattr = doc.CreateNode(XmlNodeType.Attribute, anode.Name, string.Empty);
        xattr.Value = anode.Value;
        xnew.Attributes.SetNamedItem(xattr);
      }

      if (node.HasChildren)
      {
        foreach (ConfigSectionNode cnode in node.Children)
          buildDocNode(doc, xnew, cnode);
      }

      xnew.AppendChild(doc.CreateTextNode(node.Value));
    }

    #endregion
  }
}
