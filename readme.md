# EarlyUpdateCheck
[![Version](https://img.shields.io/github/release/rookiestyle/earlyupdatecheck)](https://github.com/rookiestyle/earlyupdatecheck/releases/latest)
[![Releasedate](https://img.shields.io/github/release-date/rookiestyle/earlyupdatecheck)](https://github.com/rookiestyle/earlyupdatecheck/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/rookiestyle/earlyupdatecheck/total?color=%2300cc00)](https://github.com/rookiestyle/earlyupdatecheck/releases/latest)
[![License: GPL v3](https://img.shields.io/github/license/rookiestyle/earlyupdatecheck)](https://www.gnu.org/licenses/gpl-3.0)

Keepass performs its update check only after all initialization is done and - if configured - the most recently used database has been opened.

This KeePass plugin checks for updates of KeePass and installed plugins BEFORE a database will be opened.
The update check will be performed only if all of the following criteria are met
* "Check for update at KeePass startup" is active
* "Remember and automatically open last used database on start" is active
* "Start minimized and locked" is *not* active

EarlyUpdateCheck offers an optional one click update mode for all of my plugins integrated in KeePass' update check. 
This will invoke Windows UAC if required to copy the downloaded files into KeePass' plugin folder. 
Details can be found in the configuration settings.

For translation create a "Translations" folder in your plugin folder and copy the respective file.
Additional translations are always welcome.

# Configuration
EarlyUpdateCheck integrates into KeePass' options form.
![Options](images/EarlyUpdateCheck%20options.png)

The upper part defines whether the update check shall start even before a database is opened.
In this  case, the update check is triggered as early as possible.
As the update check runs in a separate thread, it is stll possible that you opened a database while the update check is still running.

To avoid this, check *Update check in foregound*. KeePass will then wait for the update check to be finished.
In case of network issues you may have this update check continue in background.

![Checking](images/EarlyUpdateCheck%20checking.png)

# One-Click plugin update
The lower part of the options screen allow you to update my plugins in a very easy way.
If an update for any of my plugins is available, the *Update check* form will show an additional column.
Select all plugins you wish to update and click *Start Update".
![Update](images/EarlyUpdateCheck%20One%20Click%20Update%201.png)

Once the plugins have been updated, you need to restart KeePass.
![Restart](images/EarlyUpdateCheck%20One%20Click%20Update%202.png)

# Translations
My plugins are provided with english language built-in and allow usage of translation files.
These translation files need to be placed in a folder called *Translations* inside in your plugin folder.
You're welcome to add additional translation files by createing a pull request.

Naming convention for translation files: `<plugin name>.<language identifier>.language.xml`
Example: `EarlyUpdateCheck.de.language.xml`
  
The language identifier in the filename must match the language identifier inside the KeePass language that you can select using *View -> Change language..."
This identifier is shown there as well.
