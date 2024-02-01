# Optitrack_Agent

FILL ME HERE: Project Description

TODO:

- Continue to develop TCP/IP commands based on the requirements
- Need to seperate as a class file to manage optitrack (originally developed in C# console based but migrated to Unity. clean up and encapsulation)
- Applying motion capture data to Avatar (Unity)

Updates

@2/1/2024

- Fix live logging window
- clean up codes


## TCP/IP Command

 | Member methods | Description | Parameters | Return |
| ----------- | ----------- | ----------- | ----------- |
| EchoSocket | echo to server | none | none |
| ServerDescription | Get a basic server description | none | AppVersion, Isconnected |
| ResetOrigin | Reset position and rotation to the subject | Rigidbody ID | 1 or 0 |
| GetPosition | Current position and/or rotation | 'RigidBody' or 'Markers' and Stream ID | position, rotation |
| EnableAsset | Enables tracking of corresponding asset (rigid body / skeleton) from Motive | Asset name | . |
| DisableAsset | Disables tracking of corresponding asset (rigid body / skeleton) from Motive | Asset name | . |
| getAllMarkers | . | . | . |
| startTracking | . | . | . |
| endTracking | . | . | . |

## Class/Function Reference

 | Member methods | Description | Parameters | Return |
| ----------- | ----------- | ----------- | ----------- |
| . | . | . | . |
| . | . | . | . |
| . | . | . | . |
| . | . | . | . |
| . | . | . | . |
| . | . | . | . |
| . | . | . | . |
