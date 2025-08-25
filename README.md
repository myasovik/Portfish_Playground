# Chess Adventure - Portfish Engine Integration Source

This repository contains the source code for the chess engine integration in **Chess Game**, as required by the GNU General Public License v3 (GPLv3).

## License
The code in this repository, including the modified Portfish engine, is licensed under the **GNU GPLv3**. The full license text is provided in the `Licenses/` folder.

## Overview
This is a minimal Unity project that demonstrates how **Chess Game** integrates with the Portfish chess engine. It includes:
- The complete Portfish engine source code.
- The custom `PortfishManager` communication class.
- The inteface class `UnityPlug`.
- A sample scene (`PortfishIntegrationTest.unity`) showing how to send commands to the engine and receive results.

## Important Note on Buildability
This project is a subset of the full game. The full game includes additional proprietary assets (art, sound, etc.) purchased from the Unity Asset Store, whose licenses **do not permit public redistribution**.

Therefore, this repository **does not contain all files necessary to build the complete, published version of Chess Game**.

To build a functional version of this integration demo, open this project in Unity. The core scene provided here is fully functional and will compile.