# EchoRewind

### Overview

EchoRewind is a patcher designed for the Quest client of the game EchoVR, enabling seamless interfacing with EchoRelay servers. This application simplifies the patching process, allowing users to connect to EchoVR servers on Quest.

### Installation & Usage

1. **Download JDK Development Kit:** [JDK Development Kit](https://www.oracle.com/java/technologies/downloads/#jdk21-windows).

2. **Download EchoRewind executable:** Obtain the latest release of EchoRewind from the [release page](/releases).

3. **Download the second to last EchoVR release for Quest:** [EchoVR release for Quest](https://oculusdb.rui2015.me/id/2215004568539258). (Also download OBBs for later use)

4. **Place all files in the same directory:** Ensure that the EchoRewind executable, a `config.json` file containing server details, and the EchoVR game APK are placed in the same directory.

5. **Patch the game client:** Drag and drop the EchoVR game APK onto the EchoRewind executable.

6. **Sideload the patched APK:** After patching, sideload the newly patched APK and the previously downloaded OBB file onto your Quest using a tool such as [SideQuest](https://sidequestvr.com/) and launch as normal.

### Configuration

The `config.json` file is crucial for specifying the EchoRelay server details. Please download the `config.json` directly from your server provider.

### Disclaimer

EchoRewind is a third-party application and is not affiliated with EchoVR or [EchoRelay](https://github.com/Xenomega/EchoRelay). Use it at your own risk, and be aware that modifications to game clients violate the terms of service of EchoVR.

### License

This project is licensed under the MIT License.

For more information about EchoRelay, visit the [EchoRelay GitHub Repository](https://github.com/Xenomega/EchoRelay).

Happy EchoVR gaming! 🚀

