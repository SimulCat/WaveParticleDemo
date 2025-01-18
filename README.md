# Wave/Particle Demo

This repository contains a collection of Unity standalone assets and example scene demonstrating how to build and recreate the wave and particle simulations as used in a series of VRChat worlds published by K_Cat (KilkennyCat).
Unlike the VRChat version of this repository the scripts employ C# and do not require the VRChat SDK or UdonSharp (U#).

## Technical notes:
Because GPU Compute shaders are not permitted in VRChat world projects, the assets here were built using a relatively obscure component called a Custom Render Texture (CRT).
This technique allows numerical solutions to be run on the GPU, with the output passing directly to display shaders on the GPU without requiring data to pass via the CPU.

Another benefit is that, like a compute shader, the CRT can be run selectively, updating only when required, not every frame. This results in a very efficient model that allows these simulations to run on mobile devices and standalone VR headsets. 

The main limitation at this stage is that the project utilizes shaders for the Unity Built-in Render Pipeline. I aim to port these assets to the Universal Render Pipeline in future to make these assets available to other VR/XR creators.   

Currently, the repository contains two pre-configured demonstration scenes: ParticlePanel, and WavePanel. The two scenes illustrate how to build separate particle and wave simulations.

## Prerequisite

Begin a new UNITY project using the Built-in render pipeline and add the TextMeshPro package (just the essentials), as this is used in the demo user interface.

## Usage

With the project established, clone this repository into the Assets folder.

Once the new assets are imported, two scenes should be available in the repository's scenes folder:
- WavePanel: This contains an example of the 'trippy' wave interference panel as used in some of my worlds. The simulation uses a custom render texture to calculate a wave interference pattern's real and imaginary components in a single pass. The output texture is then displayed by a custom fragment shader that scrolls the phase to provide the illusion of wave motion.
![Double Slit Wave Pattern](https://github.com/SimulCat/simulcat.github.io/blob/main/phasedemo/waveamber.gif)
- ParticlePanle: This simulation comprises two overlaid quantum scattering simulations generated from the same quantum scattering model.
  1. A particle scattering simulation that operates in two modes, pulsed and continuous.
  2. A probability density overlay.
![Double Slit Particle Pattern](https://github.com/SimulCat/simulcat.github.io/blob/main/phasedemo/particleblue.gif)
