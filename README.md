Discord Rich Presence shows what state a game or player is in, be it in a lobby, queue, or a match of certain kind.
This project, "Discord RichER Presence" retrofits games that do not have that functionality built-in.
Games currently supported are Red Dead Redemption 2, and thats about it for now :-)
Contributions like bugfixes or support for more games are welcome.

INSTALL INSTRUCTIONS<p>
  
0) Accept the fact that this project does not have a proper installer yet.
1) Create the folder "C:\Program Files\RicherPresence". Create exactly this path, otherwise other paths have to be adjusted later down the road.
2) Download the latest release from here: https://github.com/plengauer/RicherPresence/releases/latest
3) Unzip the archive into "C:\Program Files\RicherPresence". All the files, including the "RicherPresence.exe" must be right in that folder.
4) Open the "Windows Task Scheduler" with Windows Key -> Search Bar -> "Task Scheduler"
5) On the right hand side of the Task Scheduler App, click "Import Task" and choose "C:\Program Files\RicherPresence\RicherPresence.xml"
6) In "General", use the option "Change User or Group" to adjust it to your own user.
7) In "Triggers", use the option "Edit" -> "Change User" to adjust it to your own user.
8) Click "OK"
9) Either (A) restart your computer or (B) search the "Richer Presence" task in the list and click "Run".
10) Enjoy
