<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="v1search.aspx.cs" Inherits="V1Search.SearchPage" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>VersionOne Search</title>
    <link href="v1search.css" type="text/css" rel="stylesheet"/>
</head>
<body>
    <form id="form1" runat="server">
<!-- Get Search Terms -->
        <table border="0px">
            <tr>
                <td style="width: 190px"></td>
                <td style="width: 600px" align="center"><h1>VersionOne Search</h1></td>
                <td style="width: 175px">&nbsp;</td>
            </tr>        
            <tr>
                <td style="width: 190px">&nbsp;</td>
                <td style="width: 600px"><asp:TextBox ID="QueryString" runat="server" Width="600px"></asp:TextBox></td>
                <td style="width: 175px"><a href="http://lucene.apache.org/java/2_3_0/queryparsersyntax.html" target="_blank">Search Help</a></td>
            </tr>
            <tr>
                <td style="width: 190px">&nbsp;</td>
                <td style="width: 600px" align="center"><asp:Button ID="SearchButton" runat="server" Text="Search" OnClick="SearchButton_Click" /></td>
                <td style="width: 175px">&nbsp;</td>
            </tr>            
        </table>        
<!-- Summary -->
		<table class="summaryTable" cellspacing="0" cellpadding="0">
			<tr>
				<td>
					<p class="summaryData"><asp:label id="LabelSummary" runat="server" Text="<%# Summary %>"></asp:label></p>
				</td>
			</tr>
		</table>
<!-- Results -->
            <asp:repeater id="Results" runat="server" DataSource="<%# SearchResults %>">
                <HeaderTemplate>
                    <table>
                </HeaderTemplate>
				<ItemTemplate>				
				    <tr class='<%# DataBinder.Eval(Container.DataItem, IS_CLOSED_COLUMN)%>'>
				        <td>
					        <a href='<%# DataBinder.Eval(Container.DataItem, URL_COLUMN)%>' class="link"><%# DataBinder.Eval(Container.DataItem, ID_COLUMN)%></a>
					    </td>
					    <td>
						    <%# DataBinder.Eval(Container.DataItem, NAME_COLUMN)%>
					    </td>						
                    </tr>
				</ItemTemplate>
				<FooterTemplate>
                    </table>
                </FooterTemplate>
			</asp:repeater>
<!-- Paging -->
<% if(ResultPageCount > 0) {%>
		    <p class="paging">Result page:
			<asp:repeater id="ResultPage" runat="server" DataSource="<%# Paging %>">
				<ItemTemplate>
					<%# DataBinder.Eval(Container.DataItem, "html") %>&nbsp;
				</ItemTemplate>
			</asp:repeater></p>
	<%
}%>
<!-- Credits -->
		<p class="credits"><a href="http://incubator.apache.org/lucene.net/" target="_blank">Search Powered by <b>Lucene.NET</b></a></p>
      </form>
</body>
</html>
