# SC Leaks P4K Buster

![Build Status](http://bamboo001.neurotecstudio.com:8085/plugins/servlet/wittified/build-status/SCLEAK-P4KB)
![GitHub Deployment](http://bamboo001.neurotecstudio.com:8085/plugins/servlet/wittified/deploy-status/17039361)

SC Leaks P4K Buster is a tool written in C#/.NET by Heracles421 and Aftokinito that allows for the extraction of game files contained inside the Data.p4k file used by Star Citizen 3.0 and above.

## Usage

Place SCLeaksP4KBuster.exe, libzstd.dll and ZstdNet.dll next to your Data.p4k file and run SCLeaksP4KBuster.exe, the extraction will start immediately.

> **Note:**
>
> - The Data.p4k file is already really big when compressed and uncompressed it is even bigger, make sure you have enough disk space to perform the whole extraction before starting (around 150GB should be on the safe size but it might vary as CIG releases new versions of the game).
> 
## Credits

 - Heracles421: Found the exact structure of the P4K format and did an incredible rewrite of the original code.
 - Hater115: Found the compression algorithm used on the P4K format.
 - Alluran/Dino: Crybabied/bullshitted a lot and provided a lot of laughs and inside jokes to the SC Leaks community
