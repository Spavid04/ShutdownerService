
# Shutdowner
Windows service that shuts down the computer when a "magic packet" is received

### Why?
Often times I need my computer awake and running, so as to grab files from it remotely. Starting it is easy: WOL. Shutting it back down? That's a bit clunkier. Sure, I can use RDP to connect from my phone, but often times that's not the best solution: bad network, lack of time, general unwieldiness of a phone screen, etc.

### The Solution!
A service that runs regardless of a user being logged on, and listening to "magic packets" (loosely akin to WOL). It's stupidly simple, consumes virtually no resources while idle, and provides a decent level of security with some poor-man's-time-based-one-time-passwords.

### Building
Configured to use .NET 4.8 and VS2019, and will most likely work with at least .NET 4.5.
Uses the [ini-parser](https://github.com/rickyah/ini-parser) nuget module - MIT license.

### Installing
Either build it yourself, or download already built binaries from the Github Releases page.

Start up a cmd.exe with administrator privileges and use either method:
* the preferred method, using installutil.exe: `installutil.exe "path to Shutdowner.exe"`; be sure to use the 64-bit version of installutil.exe if Shutdowner.exe is a 64-bit executable
* the classic method, using sc: `sc.exe create "ShutdownerService" binpath= "path to Shutdowner.exe" start= auto obj= LocalSystem`

I would recommend write-protecting the directory and read-protecting the `config.ini` file where Shutdowner.exe resides, after installing it.

### Uninstalling
Start up a cmd.exe with administrator privileges and use either method:
* `installutil.exe /u "path to Shutdowner.exe"`; be sure to use the 64-bit version of installutil.exe if Shutdowner.exe is a 64-bit executable
* `sc.exe stop "ShutdownerService'` and then `sc.exe delete "ShutdownerService"`

### Using It
The service tries to load settings from the `config.ini` file. Here's an example containing all the possible keys and their default values:
```ini
[Shutdowner]
;Port on which to listen for packets
port=9999

;A passkey that has to be included in the hash calculation
passkey=shutdowner passkey

;Max amount of seconds allowed between the (actual local time) - (sender's time + network delay)
maxDelaySeconds=5

;Max amount of seconds the sender's time is allowed to be ahead of the local time
maxAheadSeconds=5
```

Right after installation, the service will not be running already, even though Automatic start is selected. You can start it with `sc.exe start "ShutdownerService"` from an elevated command prompt.

The service then listens on the specified port for UDP packets containing exactly a single hex string representing the SHA256 hash of the timed password. The timed password is calculated as: `{passkey}-{UTC now in 'yyyy-MM-dd HH:mm:ss' format}`, no newlines or other characters at the end. As configured with the max*Seconds keys, "UTC now" is allowed to vary a bit.

A bash example that works on my [Termux](https://f-droid.org/en/packages/com.termux/)+[Termux:Widget](https://f-droid.org/en/packages/com.termux.widget/) Android phone:
```bash
ip="computer ip"
port="9999"
passkey="shutdowner passkey"

dt=$(date -u "+%Y-%m-%d %H:%M:%S")

combined="${passkey}-${dt}"
hashed=($(echo -n $combined | sha256sum))

# many nc binaries don't allow -w0; use -w1 if yours complains
echo -n $hashed | nc -w0 -u $ip $port
```
