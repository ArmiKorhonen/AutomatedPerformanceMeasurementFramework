# AutomatedPerformanceMeasurementFramework

This repository contains the code, data, and resources related to the thesis titled "Optimization of Particle Effects in XR Environments Using Unity3D." The thesis investigates the performance implications and optimization strategies for particle systems in VR and AR applications, using Unity3D's built-in particle system and the Visual Effect Graph.

## Contents

- **Code**: Contains the necessary Unity3D scripts for the automated testing framework used in the thesis.
- **NotebooksAndData**: Includes raw and processed data from the performance tests, along with the Jupyter Notebooks used to analyze the data
- **Thesis**: Provides the original thesis as a PDF file.

## Code file descriptions

**RefreshRateSetter**:
The RefreshRateSetter script is designed to optimize the display performance of the Meta Quest headset by setting its refresh rate to the highest supported value. This script is particularly useful for VR applications where a higher refresh rate can significantly enhance the user experience by providing smoother visuals. Although the Meta Quest supports up to 120Hz, this script currently sets the refresh rate to 90Hz. It logs the success or failure of the operation, ensuring users are informed about the refresh rate status.

**MovementRecorder**
The MovementRecorder script records the movement of the VR headset relative to a specified anchor object within the scene, ensuring consistent playback regardless of changes in the user's real-world position. Additionally, it supports optional tracking of VR controllers by uncommenting the relevant lines in the code. Recorded data is saved as a CSV file, providing a timestamped record of positional and rotational data for each frame.
