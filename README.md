# Epic Online Services Integration Sample
A sample integration of the EOS P2P Relay and coherence networking for player-hosted servers.

ℹ️ **Download a release that matches the installed coherence SDK version to at least MAJOR.MINOR. Check the list of [releases](https://github.com/coherence/steam-integration-sample/releases) on this repository.**

## How does it work

The **EOS Integration** is using a combination of the Epic Online Services relay servers and an implementation of `ICoherenceRelay` to enable Epic Online Services users to connect and play with each other while avoiding NAT issues.
The `ICoherenceRelay` implementation allows for users to connect to the hosting client through Epic Online Services, and have the client forward their data packets to the user-hosted Replication Server.

## PlayEveryWare EOS Unity Plugin

The **EOS Integration** embeds the [PlayEveryWare EOS Unity Plugin](https://github.com/PlayEveryWare/eos_plugin_for_unity) package. Check the PlayEveryWare repository for instructions on how to get started with setting up an Epic Games Store Product to be able to use this Integration Sample.

## Epic Online Services Dev Auth Tool

The **EOS Integration** uses the [Epic Online Services Dev Auth Tool](https://dev.epicgames.com/docs/epic-account-services/developer-authentication-tool) to login with the Epic Online Services. The Dev Auth Tool is a standalone application from Epic that is NOT included in this Sample. It is part of the EOS SDK that you can download from your [Epic Developer Portal site](https://dev.epicgames.com/). You can find a full guide on how to use the Dev Auth Tool in the [PlayEveryWare documentation](https://github.com/PlayEveryWare/eos_plugin_for_unity/blob/development/com.playeveryware.eos/Documentation%7E/Walkthrough.md)

## Components

`CoherenceEOSManager` will initialize the EOSManager and manage logging in with Epic, and the joining and hosting of self-hosted servers.

`EOSSampleUI` is a simple sample for hosting and joining accessibility, that will provide a UI in the top left of the screen.

## Using the SampleScene

When you first launch the SampleScene, you will be asked to input your Dev Auth Tool name to be able to login with Epic.

Upon successful login, you will be able to start hosting a game, or join an existing one.

When you start hosting a game, on the top left of your screen you will your ProductUserId, you can copy this string to the clipboard.

In order to join an existing game, you will need the ProductUserId from the host, then click join and you will be part of the existing session.

You can move the cube with WASD.

## How to use in your own project

1. Copy the "EOSSample" folder into your own project.
1. In the scene containing your `CoherenceBridge` Gameobject/Component, create a new Gameobject with the `CoherenceEOSManager` and `EOSManager` Components.
1. (Optional) Add the `EOSSampleUI` to the same Gameobject.
1. Use the Inspector with the `CoherenceEOSManager` to set the `Dev Auth Tool Port` where you are hosting your Dev Auth Tool.

You can now use the integration through either the `EOSSampleUI`, or the `CoherenceEOSManager` right-click menu in the Inspector.
