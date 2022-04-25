# scpbot
A discord bot that looks up SCP entries

## The bot
Sending a message containing the word `scp` (case insensitive)
causes the bot to check for any entry numbers or entry titles in the message,
and responds with a list of all matching SCPs.

A title must be enclosed in `"` or `'`.

## Hosting
A systemD service file is included (`scpbot.service`).

To use it, build to `/srv/scpbot` by using the `./install.sh` script
and create a file named `token` containing your Discord bot token in the same directory.

If you want to write your own hosting scripts, note that the bot scrapes the SCP wiki at startup and doesn't refresh its index, so it should be periodically restarted to receive newer entries.

## Configuration
To configure the bot, use the `config.json` file.
Its fields are:

### MinSearchResults
Sets how many search results are returned at minimum when searching for titles.

If an exact match is found, this value is ignored and only that entry is given.

### MaxSearchResults
Sets how many search results are returned at most.

### DeleteCost
Sets the cost of omitting a search term from a title search.
These 3 cost entries are used for a modified [Levenshtein algorithm](https://en.wikipedia.org/wiki/Levenshtein_distance).

### InsertCost
Sets the cost of including an unrelated search term in a title search.

### ReplaceCost
Sets the 'cost' of replacing a search term during title search.
