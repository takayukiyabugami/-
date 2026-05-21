<?xml version="1.0" encoding="shift_jis"?>
<xsl:stylesheet version="1.0"
     xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="html" version="4.01" indent="yes" />
<xsl:param name="syurui">99999999999</xsl:param>
<xsl:param name="serial">99999999999</xsl:param>
<xsl:param name="utisyu">99999999999</xsl:param>
<xsl:template match="/">
    <HTML>
    <HEAD>
        <script type="text/JScript">
        //<![CDATA[
            function set_syurui(syurui_no){
                var syurui_val=document.From1.wt_syurui[syurui_no].value;
                    var userAgent = window.navigator.userAgent.toLowerCase();

		    if(userAgent.indexOf('msie') != -1 || userAgent.indexOf('trident') != -1)
		    {
		      // IE
                         // Load XSL
                    var xslt=new ActiveXObject("MSXML2.XSLTemplate");

                    var xsldoc=new ActiveXObject("MSXML2.FreeThreadedDOMDocument");
                    xsldoc.async = false;
                    xsldoc.load("MEET05.XSL");

                         // Load XML
                    var xmldoc=new ActiveXObject("MSXML2.DOMDocument");
                       xmldoc.async = false;
                    xmldoc.load("MEET.XML");

                         // Transform
                         xslt.stylesheet = xsldoc;

                    var xslproc = xslt.createProcessor();
                    xslproc.input = xmldoc;
                         if(syurui_val == '全体'){
                             xslproc.addParameter("syurui","99999999999");
                             xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","99999999999");
                         }else if(syurui_val == '指示'){
                             xslproc.addParameter("syurui","打合せ簿");
                             xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","指示");
                         }else if(syurui_val == '協議'){
                             xslproc.addParameter("syurui","打合せ簿");
                             xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","協議");
                         }else if(syurui_val == '通知'){
                             xslproc.addParameter("syurui","打合せ簿");
                             xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","通知");
                         }else if(syurui_val == '承諾'){
                             xslproc.addParameter("syurui","打合せ簿");
                             xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","承諾");
                         }else if(syurui_val == '提出'){
                             xslproc.addParameter("syurui","打合せ簿");
                             xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","提出");
                         }else if(syurui_val == '提示'){
                             xslproc.addParameter("syurui","打合せ簿");
                             xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","提示");
                         }else if(syurui_val == '報告'){
                             xslproc.addParameter("syurui","打合せ簿");
                             xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","報告");
                         }else if(syurui_val == '届出'){
                             xslproc.addParameter("syurui","打合せ簿");
                             xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","届出");
                         }else if(syurui_val == '確認'){
                             xslproc.addParameter("syurui","打合せ簿");
                             xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","確認");
                         }else if(syurui_val == '立会'){
                             xslproc.addParameter("syurui","打合せ簿");
                             xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","立会");
                         }else if(syurui_val == 'その他'){
                             xslproc.addParameter("syurui","打合せ簿");
                             xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","その他");
                         }else{
                    xslproc.addParameter("syurui",syurui_val);
                    xslproc.addParameter("serial","99999999999");
                             xslproc.addParameter("utisyu","99999999999");
                         };
                    xslproc.transform();
                    var resultData=xslproc.output;
                    document.getElementById("showArea").innerHTML=resultData;

		    }
                     else
                    {
                         var xslproc = new XSLTProcessor();
                         // load the xsl file
                         var myXMLHTTPRequest = new XMLHttpRequest();
                         myXMLHTTPRequest.open("GET", "MEET05.XSL", false);
                         myXMLHTTPRequest.send(null);

                         // get the XML document
                         var xsldoc = myXMLHTTPRequest.responseXML;
                         xslproc.importStylesheet(xsldoc);

                         // load the xml file
                         myXMLHTTPRequest = new XMLHttpRequest();
                         myXMLHTTPRequest.open("GET", "MEET.XML", false);
                         myXMLHTTPRequest.send(null);
                         var xmldoc = myXMLHTTPRequest.responseXML;
                         
                         // set the parameters
                         if(syurui_val == '全体'){
                             xslproc.setParameter(null,"syurui","99999999999");
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","99999999999");
                         }else if(syurui_val == '指示'){
                             xslproc.setParameter(null,"syurui","打合せ簿");
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","指示");
                         }else if(syurui_val == '協議'){
                             xslproc.setParameter(null,"syurui","打合せ簿");
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","協議");
                         }else if(syurui_val == '通知'){
                             xslproc.setParameter(null,"syurui","打合せ簿");
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","通知");
                         }else if(syurui_val == '承諾'){
                             xslproc.setParameter(null,"syurui","打合せ簿");
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","承諾");
                         }else if(syurui_val == '提出'){
                             xslproc.setParameter(null,"syurui","打合せ簿");
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","提出");
                         }else if(syurui_val == '提示'){
                             xslproc.setParameter(null,"syurui","打合せ簿");
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","提示");
                         }else if(syurui_val == '報告'){
                             xslproc.setParameter(null,"syurui","打合せ簿");
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","報告");
                         }else if(syurui_val == '届出'){
                             xslproc.setParameter(null,"syurui","打合せ簿");
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","届出");
                         }else if(syurui_val == '確認'){
                             xslproc.setParameter(null,"syurui","打合せ簿");
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","確認");
                         }else if(syurui_val == '立会'){
                             xslproc.setParameter(null,"syurui","打合せ簿");
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","立会");
                         }else if(syurui_val == 'その他'){
                             xslproc.setParameter(null,"syurui","打合せ簿");
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","その他");
                         }else{
                             xslproc.setParameter(null,"syurui",syurui_val);
                             xslproc.setParameter(null,"serial","99999999999");
                             xslproc.setParameter(null,"utisyu","99999999999");
                         };
                         var fragment = xslproc.transformToFragment(xmldoc, document);
                         document.getElementById('showArea').innerHTML = "";
                         document.getElementById('showArea').appendChild(fragment);
                    }
            }

            function set_serial(serial_no){
                serial_length = document.From2.wt_serial.length;
                var syurui_name = "";

                for (j = 0; j < serial_length; j ++) {
                    if(document.From2.wt_serial[j].value == serial_no){
                        syurui_name = document.From2.wt_syurui[j].value;
                    }
                }

                var userAgent = window.navigator.userAgent.toLowerCase();

                if(userAgent.indexOf('msie') != -1 || userAgent.indexOf('trident') != -1)
                {
                   // IE

                   // Load XSL
                    var xslt=new ActiveXObject("MSXML2.XSLTemplate");

                    var xsldoc=new ActiveXObject("MSXML2.FreeThreadedDOMDocument");
                    xsldoc.async = false;
                    xsldoc.load("MEET05.XSL");

                   // Load XML
                    var xmldoc=new ActiveXObject("MSXML2.DOMDocument");
                       xmldoc.async = false;
                    xmldoc.load("MEET.XML");

                   // Transform
                   xslt.stylesheet = xsldoc;

                    var xslproc = xslt.createProcessor();
                    xslproc.input = xmldoc;
                    xslproc.addParameter("syurui",syurui_name);
                    xslproc.addParameter("serial",serial_no);
                    xslproc.transform();
                    var resultData=xslproc.output;
                    document.getElementById("showArea").innerHTML=resultData;

                }
                else
		{
                    var xslproc = new XSLTProcessor();
                    // load the xsl file
                    var myXMLHTTPRequest = new XMLHttpRequest();
                    myXMLHTTPRequest.open("GET", "MEET05.XSL", false);
                    myXMLHTTPRequest.send(null);

                    // get the XML document
                    var xsldoc = myXMLHTTPRequest.responseXML;
                    xslproc.importStylesheet(xsldoc);

                    // load the xml file
                    myXMLHTTPRequest = new XMLHttpRequest();
                    myXMLHTTPRequest.open("GET", "MEET.XML", false);
                    myXMLHTTPRequest.send(null);
                    var xmldoc = myXMLHTTPRequest.responseXML;

                    // set the parameters
                    xslproc.setParameter(null, "syurui",syurui_name);
                    xslproc.setParameter(null, "serial",serial_no);
                    var fragment = xslproc.transformToFragment(xmldoc, document);
                    document.getElementById('showArea').innerHTML = "";
                    document.getElementById('showArea').appendChild(fragment);
                }
            }

        //]]>
        </script>
    </HEAD>
    <BODY>

    <DIV id="showArea">
        <TABLE bgcolor="#ffffff" cellspacing="0" cellpadding="0" border="0">
            <TR>
                <TD valign="top" align="left" width="100">
                    <xsl:call-template name="フォルダ表示"/>
                </TD>
                <TD width="20">　</TD>
                <TD valign="top" align="left" width="680">
                    <TABLE bgcolor="#ffffff" cellspacing="0" cellpadding="0" border="0">
                        <TR>
					                <TD>
                            <xsl:apply-templates select="meetdata"/>
					                </TD>
                        </TR>
                    </TABLE>
                </TD>
            </TR>
        </TABLE>
    </DIV>

    </BODY>
    </HTML>
</xsl:template>

<xsl:template name="フォルダ表示">
    <FORM name="From1">
    <TABLE bgcolor="#ffffff" cellspacing="5" cellpadding="3" border="0">
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="全体" />
        <xsl:if test="'99999999999' = $syurui">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="left">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(0)
                </xsl:attribute>
                全体
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'99999999999' != $syurui">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="left">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(0)
                </xsl:attribute>
                全体
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="打合せ簿" />
        <xsl:if test="'打合せ簿' = $syurui">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="left">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(1)
                </xsl:attribute>
                打合せ簿
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'打合せ簿' != $syurui">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="left">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(1)
                </xsl:attribute>
                打合せ簿
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="指示" />
        <xsl:if test="'指示' = $utisyu">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(2)
                </xsl:attribute>
                指示
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'指示' != $utisyu">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(2)
                </xsl:attribute>
                指示
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="協議" />
        <xsl:if test="'協議' = $utisyu">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(3)
                </xsl:attribute>
                協議
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'協議' != $utisyu">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(3)
                </xsl:attribute>
                協議
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="通知" />
        <xsl:if test="'通知' = $utisyu">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(4)
                </xsl:attribute>
                通知
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'通知' != $utisyu">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(4)
                </xsl:attribute>
                通知
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="承諾" />
        <xsl:if test="'承諾' = $utisyu">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(5)
                </xsl:attribute>
                承諾
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'承諾' != $utisyu">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(5)
                </xsl:attribute>
                承諾
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="提出" />
        <xsl:if test="'提出' = $utisyu">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(6)
                </xsl:attribute>
                提出
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'提出' != $utisyu">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(6)
                </xsl:attribute>
                提出
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="提示" />
        <xsl:if test="'提示' = $utisyu">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(7)
                </xsl:attribute>
                提示
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'提示' != $utisyu">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(7)
                </xsl:attribute>
                提示
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="報告" />
        <xsl:if test="'報告' = $utisyu">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(8)
                </xsl:attribute>
                報告
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'報告' != $utisyu">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(8)
                </xsl:attribute>
                報告
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="届出" />
        <xsl:if test="'届出' = $utisyu">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(9)
                </xsl:attribute>
                届出
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'届出' != $utisyu">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(9)
                </xsl:attribute>
                届出
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="確認" />
        <xsl:if test="'確認' = $utisyu">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(10)
                </xsl:attribute>
                確認
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'確認' != $utisyu">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(10)
                </xsl:attribute>
                確認
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="立会" />
        <xsl:if test="'立会' = $utisyu">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(11)
                </xsl:attribute>
                立会
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'立会' != $utisyu">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(11)
                </xsl:attribute>
                立会
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="その他" />
        <xsl:if test="'その他' = $utisyu">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(12)
                </xsl:attribute>
                その他
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'その他' != $utisyu">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="right">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(12)
                </xsl:attribute>
                その他
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="工事履行報告書" />
        <xsl:if test="'工事履行報告書' = $syurui">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="left">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(13)
                </xsl:attribute>
                履行報告書
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'工事履行報告書' != $syurui">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="left">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(13)
                </xsl:attribute>
                履行報告書
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="段階確認書" />
        <xsl:if test="'段階確認書' = $syurui">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="left">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(14)
                </xsl:attribute>
                段階確認書
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'段階確認書' != $syurui">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="left">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(14)
                </xsl:attribute>
                段階確認書
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="材料確認願" />
        <xsl:if test="'材料確認願' = $syurui">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="left">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(15)
                </xsl:attribute>
                材料確認願
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'材料確認願' != $syurui">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="left">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(15)
                </xsl:attribute>
                材料確認願
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <INPUT TYPE="hidden" NAME="wt_syurui" VALUE="確認・立会願" />
        <xsl:if test="'確認・立会願' = $syurui">
            <TR>
            <TD bgcolor="#cccccc" ALIGN="left">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(16)
                </xsl:attribute>
                確認・立会
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
        <xsl:if test="'確認・立会願' != $syurui">
            <TR>
            <TD bgcolor="#FFFFFF" ALIGN="left">
                <xsl:element name="a">
                <xsl:attribute name="href">
                    javascript:set_syurui(16)
                </xsl:attribute>
                確認・立会
                </xsl:element>
            </TD>
            </TR>
        </xsl:if>
    </TABLE>
    </FORM>
</xsl:template>

<xsl:template match="meetdata">
    <FORM name="From2">

    <TABLE bgcolor="#AAAAAA" cellspacing="0" cellpadding="0">
    <TR>
    <TD>

        <TABLE cellspacing="1" cellpadding="3">
        <TR>
            <TH bgcolor="#CCCCCC">発議日</TH>
            <TH bgcolor="#CCCCCC">打合せ簿タイトル</TH>
            <TH bgcolor="#CCCCCC">打合せ簿添付資料内容</TH>
            <TH bgcolor="#CCCCCC">上位</TH>
            <TH bgcolor="#CCCCCC">下位</TH>
        </TR>

        <xsl:for-each select="./打合せ簿情報">
            <xsl:sort select="./発行日付" data-type="text" order="ascending" />
            <xsl:sort select="./シリアル番号" data-type="number" order="ascending" />

            <INPUT TYPE="hidden" NAME="wt_syurui">
                <xsl:attribute name="value">
                    <xsl:value-of select="./工事帳票種類"/>
                </xsl:attribute>
            </INPUT>
            <INPUT TYPE="hidden" NAME="wt_serial">
                <xsl:attribute name="value">
                    <xsl:value-of select="./シリアル番号"/>
                </xsl:attribute>
            </INPUT>

            <xsl:if test="$syurui=99999999999">
                <TR>
                    <TD bgcolor="#FFFFFF"><xsl:value-of select="./発行日付"/></TD>
                    <TD bgcolor="#FFFFFF">
                        <A href="ORG/{./オリジナルファイル情報/打合せ簿オリジナルファイル名}" target="_blank">
                            <xsl:value-of select="./打合せ簿名称"/>
                        </A>
                    </TD>
                    <TD bgcolor="#FFFFFF">
                        <xsl:for-each select="./オリジナルファイル情報">
                            <xsl:if test="position() &gt; 1">
                                <A href="ORG/{./打合せ簿オリジナルファイル名}" target="_blank">
                                    <xsl:value-of select="./オリジナルファイル内容"/><BR />
                                </A>
                            </xsl:if>
                        </xsl:for-each>
                    </TD>
                    <TD bgcolor="#FFFFFF">
                        <xsl:element name="a">
                            <xsl:attribute name="href">
                                javascript:set_serial(<xsl:value-of select="./シリアル番号/@上位打合せ簿シリアル番号"/>)
                            </xsl:attribute>
                            <xsl:value-of select="./シリアル番号/@上位打合せ簿シリアル番号"/>
                        </xsl:element>
                    </TD>
                    <TD bgcolor="#FFFFFF">
                        <xsl:element name="a">
                            <xsl:attribute name="href">
                                javascript:set_serial(<xsl:value-of select="./シリアル番号/@下位打合せ簿シリアル番号"/>)
                            </xsl:attribute>
                            <xsl:value-of select="./シリアル番号/@下位打合せ簿シリアル番号"/>
                        </xsl:element>
                    </TD>
                </TR>
            </xsl:if>

            <xsl:choose>
                <xsl:when test="(./工事帳票種類 = $syurui) and (./打合せ簿種類 = $utisyu)">
                    <xsl:if test="(./工事帳票種類 = $syurui) and ($utisyu = ./打合せ簿種類) and ($serial = 99999999999 or ./シリアル番号 = $serial)">
                        <TR>
                        <TD bgcolor="#FFFFFF"><xsl:value-of select="./発行日付"/></TD>
                        <TD bgcolor="#FFFFFF">
                            <A href="ORG/{./オリジナルファイル情報/打合せ簿オリジナルファイル名}" target="_blank">
                                <xsl:value-of select="./打合せ簿名称"/>
                            </A>
                        </TD>
                        <TD bgcolor="#FFFFFF">
                            <xsl:for-each select="./オリジナルファイル情報">
                                <xsl:if test="position() &gt; 1">
                                    <A href="ORG/{./打合せ簿オリジナルファイル名}" target="_blank">
                                        <xsl:value-of select="./オリジナルファイル内容"/><BR />
                                    </A>
                                </xsl:if>
                            </xsl:for-each>
                        </TD>
                        <TD bgcolor="#FFFFFF">
                            <xsl:element name="a">
                                <xsl:attribute name="href">
                                    javascript:set_serial(<xsl:value-of select="./シリアル番号/@上位打合せ簿シリアル番号"/>)
                                </xsl:attribute>
                                <xsl:value-of select="./シリアル番号/@上位打合せ簿シリアル番号"/>
                            </xsl:element>
                        </TD>
                        <TD bgcolor="#FFFFFF">
                            <xsl:element name="a">
                                <xsl:attribute name="href">
                                    javascript:set_serial(<xsl:value-of select="./シリアル番号/@下位打合せ簿シリアル番号"/>)
                                </xsl:attribute>
                                <xsl:value-of select="./シリアル番号/@下位打合せ簿シリアル番号"/>
                            </xsl:element>
                        </TD>
                        </TR>
                    </xsl:if>
                </xsl:when>
                <xsl:when test="./工事帳票種類 = $syurui and ('99999999999' = $utisyu)">
                    <xsl:if test="$serial = 99999999999 or ./シリアル番号 = $serial">
                        <TR>
                        <TD bgcolor="#FFFFFF"><xsl:value-of select="./発行日付"/></TD>
                        <TD bgcolor="#FFFFFF">
                            <A href="ORG/{./オリジナルファイル情報/打合せ簿オリジナルファイル名}" target="_blank">
                                <xsl:value-of select="./打合せ簿名称"/>
                            </A>
                        </TD>
                        <TD bgcolor="#FFFFFF">
                            <xsl:for-each select="./オリジナルファイル情報">
                                <xsl:if test="position() &gt; 1">
                                    <A href="ORG/{./打合せ簿オリジナルファイル名}" target="_blank">
                                        <xsl:value-of select="./オリジナルファイル内容"/><BR />
                                    </A>
                                </xsl:if>
                            </xsl:for-each>
                        </TD>
                        <TD bgcolor="#FFFFFF">
                            <xsl:element name="a">
                                <xsl:attribute name="href">
                                    javascript:set_serial(<xsl:value-of select="./シリアル番号/@上位打合せ簿シリアル番号"/>)
                                </xsl:attribute>
                                <xsl:value-of select="./シリアル番号/@上位打合せ簿シリアル番号"/>
                            </xsl:element>
                        </TD>
                        <TD bgcolor="#FFFFFF">
                            <xsl:element name="a">
                                <xsl:attribute name="href">
                                    javascript:set_serial(<xsl:value-of select="./シリアル番号/@下位打合せ簿シリアル番号"/>)
                                </xsl:attribute>
                                <xsl:value-of select="./シリアル番号/@下位打合せ簿シリアル番号"/>
                            </xsl:element>
                        </TD>
                        </TR>
                    </xsl:if>
                </xsl:when>
            </xsl:choose>

        </xsl:for-each>
        </TABLE>

    </TD>
    </TR>
    </TABLE>

    </FORM>
</xsl:template>
</xsl:stylesheet>
