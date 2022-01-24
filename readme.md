# EarlyUpdateCheck
[![Version](https://img.shields.io/github/release/rookiestyle/earlyupdatecheck)](https://github.com/rookiestyle/earlyupdatecheck/releases/latest)
[![Releasedate](https://img.shields.io/github/release-date/rookiestyle/earlyupdatecheck)](https://github.com/rookiestyle/earlyupdatecheck/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/rookiestyle/earlyupdatecheck/total?color=%2300cc00)](https://github.com/rookiestyle/earlyupdatecheck/releases/latest/download/EarlyUpdateCheck.plgx)\
[![License: GPL v3](https://img.shields.io/github/license/rookiestyle/earlyupdatecheck)](https://www.gnu.org/licenses/gpl-3.0)

Keepass performs its update check only after all initialization is done and - if configured - the most recently used database has been opened.

EarlyUpdateCheck checks for updates of KeePass and installed plugins BEFORE a database will be opened.  
Additionally, it offers a handy one click update mode for all of my plugins integrated in KeePass' update check and also supports updating KeePass itself. 
This will invoke Windows UAC if required to copy the downloaded files into KeePass' plugin folder. 
Details can be found in the configuration settings.

EarlyUpdateCheck can update other author's plugins as well.
Please have a look at the [wiki](https://github.com/Rookiestyle/EarlyUpdateCheck/wiki/Update-other-plugins) for details.


The update check triggered by EarlyUpdateCheck will be performed only if all of the following criteria are met
* "Check for update at KeePass startup" is active
* "Remember and automatically open last used database on start" is active
* "Start minimized and locked" is *not* active


# Table of Contents
- [Configuration](#configuration)
- [One-click plugin update](#one-click-plugin-update)
- [KeePass update](#keepass-update)
- [Translations](#translations)
- [Download & updates](#download--updates)
- [Requirements](#requirements)

# Configuration
EarlyUpdateCheck integrates into KeePass' options form.
![Options](images/EarlyUpdateCheck%20options.png)

The upper part defines whether the update check shall start even before a database is opened.
In this  case, the update check is triggered as early as possible.
As the update check runs in a separate thread, it is still possible that you opened a database while the update check is still running.

To avoid this, check *Update check in foregound*. KeePass will then wait for the update check to be finished.
In case of network issues you may have this update check continue in background.\
![Checking](images/EarlyUpdateCheck%20checking.png)

# One-Click plugin update
The lower part of the options screen allow you to update my plugins in a very easy way.
If an update for any of my plugins is available, the *Update check* form will show an additional column.
Select all plugins you wish to update and click *Start Update*.\
![Update](images/EarlyUpdateCheck%20One%20Click%20Update%201.png)

Once the plugins have been updated, you need to restart KeePass.\
![Restart](images/EarlyUpdateCheck%20One%20Click%20Update%202.png)

# KeePass update
EarlyUpdateCheck can also update KeePass itself.  
KeePass is shipped in different variants and each of them requires a specific way of updating.  
- You can install KeePass using a KeePass-*Version*-Setup.exe - This is the default and linked at https://keepass.info/download.html as *Installer for Windows*
- You can run KeePass using a portable version - This is also linked at https://keepass.info/download.html  as *Portable*
- You can install KeePass using a KeePass-*Version*.msi file which is not linked at https://keepass.info/download.html but on Sourceforge only

EarlyUpdateCheck can detect the used installation variant and if it's correct, you only need to confirm and you're ready to go.  
![KeePass update](images/EarlyUpdateCheck%20KeePass%20Update.png)

When using the portable version, EarlyUpdateCheck will download the new zip package, extract it to a temporary folder and open this folder in explorer.  
To finish the update, you need to close KeePass and copy the downloaded files to your current KeePass folder.

For the other two cases, EarlyUpdateCheck will download the required file. You can decide to open the folder containing the downloaded file or you can  start the installation immediately.  
Don't forget to close KeePass in the latter case.

Anytime, please ensure to have a working backup strategy.

# Translations
My plugins are provided with English language built-in and allow usage of translation files.
These translation files need to be placed in a folder called *Translations* inside your plugin folder.
If a text is missing in the translation file, it is backfilled with English text.
You're welcome to add additional translation files by creating a pull request as described in the [wiki](https://github.com/Rookiestyle/EarlyUpdateCheck/wiki/Create-or-update-translations).

Naming convention for translation files: `<plugin name>.<language identifier>.language.xml`\
Example: `EarlyUpdateCheck.de.language.xml`
  
The language identifier in the filename must match the language identifier inside the KeePass language that you can select using *View -> Change language...*\
This identifier is shown there as well.

## Download & updates
Please follow these links to download the plugin file itself.
- [Download newest release](https://github.com/rookiestyle/earlyupdatecheck/releases/latest/download/EarlyUpdateCheck.plgx)
- [Download history](https://github.com/rookiestyle/earlyupdatecheck/releases)

If you're interested in any of the available translations in addition, please download them from the [Translations](Translations) folder.

In addition to the manual way of downloading the plugin, you can use [EarlyUpdateCheck](https://github.com/rookiestyle/earlyupdatecheck/) to update both the plugin and its translations automatically.  
See the [one click plugin update wiki](https://github.com/Rookiestyle/EarlyUpdateCheck/wiki/One-click-plugin-update) for more details.

# Requirements
* KeePass: 2.38
