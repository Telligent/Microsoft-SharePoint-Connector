var EditDocumentError_1 = "The document could not be opened for editing. A compatible application could not be found to edit the document."; var EditDocumentError_2 = "'Edit Document' requires a compatible application and web browser."; var browseris = new Browseris(); var OpenDoc = {}; var OpenDocStr = ""; var UnsupportedExtensionl = ["ascx", "asp", "aspx", "htm", "html", "master", "odc", "exe", "bat", "com", "cmd", "onetoc2"]; function editDocument(fileName) { var a = editDocumentWithNoUI(fileName); if (a == 1) { alert(EditDocumentError_1) } else if (a == 2) { alert(EditDocumentError_2) } } function editDocumentWithNoUI(fileName) { var b, l, c = "", g = false, d = "SharePoint.OpenDocuments"; if (fileName.charAt(0) == "/" || fileName.substr(0, 3).toLowerCase() == "%2f") { fileName = document.location.protocol + "//" + document.location.host + fileName } var j = CreateExtension(unescapeProperly(fileName)); if (FSupportCheckoutToLocal(j)) try { b = EnsureExtension(d + ".3"); if (b != null) { if (!b.EditDocument3(window, fileName, g, c)) return 1; return 0 } } catch (e) { } b = EnsureExtension(d); if (b != null) { try { if (!b.EditDocument2(window, fileName, c)) return 1; return 0 } catch (e) { } try { window.onfocus = null; if (CreateExtension(fileName) == "ppt" && c == "") c = "PowerPoint.Slide"; if (!b.EditDocument(fileName, c)) return 1; return 0 } catch (e) { return 2 } } return 1 } function unescapeProperly(b) { var a = null; if ((browseris.ie55up || browseris.nav6up) && typeof decodeURIComponent != "undefined") { a = decodeURIComponent(b) } else { a = unescapeProperlyInternal(b) } return a } function CreateExtension(a) { var c = new String(a); var b = /^.*\.([^\.]*)$/; return c.replace(b, "$1").toLowerCase() } function FSupportCheckoutToLocal(a) { var c = true; if (a == null || a == "") return false; a = a.toLowerCase(); for (var b = 0, b = 0; b < UnsupportedExtensionl.length; b++) { if (a == UnsupportedExtensionl[b]) { return false } } return true } function EnsureExtension(a) { if (OpenDoc == null || OpenDocStr != a) { OpenDoc = null; OpenDocStr = null; if (window.ActiveXObject) { try { OpenDoc = new ActiveXObject(a); OpenDocStr = a } catch (c) { OpenDoc = null; OpenDocStr = null } } else if (IsSupportedMacBrowser() && a.indexOf("SharePoint.OpenDocuments") >= 0) { var b = CreateMacPlugin(); if (b != null) { OpenDoc = b; OpenDocStr = "SharePoint.MacPlugin" } } else if (IsSupportedFirefoxOnWin() && a.indexOf("SharePoint.OpenDocuments") >= 0) { var b = CreateFirefoxOnWindowsPlugin(); if (b != null) { OpenDoc = b; OpenDocStr = "SharePoint.FFWinPlugin" } } } return OpenDoc } function IsSupportedMacBrowser() { return browseris.mac && (browseris.firefox3up || browseris.safari3up) } function IsSupportedFirefoxOnWin() { return (browseris.winnt || browseris.win32 || browseris.win64bit) && browseris.firefox3up } function CreateFirefoxOnWindowsPlugin() { var b = null; if (IsSupportedFirefoxOnWin()) { try { b = document.getElementById("winFirefoxPlugin"); if (!b && IsFirefoxOnWindowsPluginInstalled()) { var a = document.createElement("object"); a.id = "winFirefoxPlugin"; a.type = "application/x-sharepoint"; a.width = 0; a.height = 0; a.style.setProperty("visibility", "hidden", ""); document.body.appendChild(a); b = document.getElementById("winFirefoxPlugin") } } catch (c) { b = null } } return b } function IsFirefoxOnWindowsPluginInstalled() { return navigator.mimeTypes && navigator.mimeTypes['application/x-sharepoint'] && navigator.mimeTypes['application/x-sharepoint'].enabledPlugin } function unescapeProperlyInternal(c) { if (c == null) { return "null" } var e = 0, g = 0, d = "", f = [], a = 0, b, h; while (e < c.length) { if (c.charAt(e) == "%") { if (c.charAt(++e) == "u") { b = ""; for (g = 0; g < 4 && e < c.length; ++g) { b += c.charAt(++e) } while (b.length < 4) { b += "0" } h = parseInt(b, 16); if (isNaN(h)) { d += "?" } else { d += String.fromCharCode(h) } } else { b = ""; for (g = 0; g < 2 && e < c.length; ++g) { b += c.charAt(e++) } while (b.length < 2) { b += "0" } h = parseInt(b, 16); if (isNaN(h)) { if (a) { d += Vutf8ToUnicode(f); a = 0; f.length = a } d += "?" } else { f[a++] = h } } } else { if (a) { d += Vutf8ToUnicode(f); a = 0; f.length = a } d += c.charAt(e++) } } if (a) { d += Vutf8ToUnicode(f); a = 0; f.length = a } return d } function Browseris() { var a = navigator.userAgent.toLowerCase(); this.osver = 1; if (a) { var g = a.substring(a.indexOf("windows ") + 11); this.osver = parseFloat(g) } this.major = parseInt(navigator.appVersion); this.nav = a.indexOf("mozilla") != -1 && (a.indexOf("spoofer") == -1 && a.indexOf("compatible") == -1); this.nav6 = this.nav && this.major == 5; this.nav6up = this.nav && this.major >= 5; this.nav7up = false; if (this.nav6up) { var b = a.indexOf("netscape/"); if (b >= 0) { this.nav7up = parseInt(a.substring(b + 9)) >= 7 } } this.ie = a.indexOf("msie") != -1; this.aol = this.ie && a.indexOf(" aol ") != -1; if (this.ie) { var e = a.substring(a.indexOf("msie ") + 5); this.iever = parseInt(e); this.verIEFull = parseFloat(e) } else this.iever = 0; this.ie4up = this.ie && this.major >= 4; this.ie5up = this.ie && this.iever >= 5; this.ie55up = this.ie && this.verIEFull >= 5.5; this.ie6up = this.ie && this.iever >= 6; this.ie7down = this.ie && this.iever <= 7; this.ie7up = this.ie && this.iever >= 7; this.ie8standard = this.ie && document.documentMode && document.documentMode == 8; this.winnt = a.indexOf("winnt") != -1 || a.indexOf("windows nt") != -1; this.win32 = this.major >= 4 && navigator.platform == "Win32" || a.indexOf("win32") != -1 || a.indexOf("32bit") != -1; this.win64bit = a.indexOf("win64") != -1; this.win = this.winnt || this.win32 || this.win64bit; this.mac = a.indexOf("mac") != -1; this.w3c = this.nav6up; this.safari = a.indexOf("webkit") != -1; this.safari125up = false; this.safari3up = false; if (this.safari && this.major >= 5) { var b = a.indexOf("webkit/"); if (b >= 0) { this.safari125up = parseInt(a.substring(b + 7)) >= 125 } var f = a.indexOf("version/"); if (f >= 0) { this.safari3up = parseInt(a.substring(f + 8)) >= 3 } } this.firefox = this.nav && a.indexOf("firefox") != -1; this.firefox3up = false; this.firefox36up = false; if (this.firefox && this.major >= 5) { var d = a.indexOf("firefox/"); if (d >= 0) { var c = a.substring(d + 8); this.firefox3up = parseInt(c) >= 3; this.firefox36up = parseFloat(c) >= 3.6 } } }