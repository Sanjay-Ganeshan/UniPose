import setuptools

with open("README.md", "r") as fh:
    long_description = fh.read()

# We also need Pytorch, but pip can't install this

setuptools.setup(
    name="unipose", # Replace with your own username
    version="1.0.5",
    author="Julie Ganeshan",
    author_email="HeavenlyQueen@outlook.com",
    description="2D Pose in Unity with Python backend",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/Sanjay-Ganeshan/UniPose",
    packages=setuptools.find_packages(),
    classifiers=[
        "Programming Language :: Python :: 3",
        "License :: OSI Approved :: MIT License",
        "Operating System :: Microsoft :: Windows :: Windows 10",
    ],
    python_requires='>=3.7',
    install_requires=[
        "opencv-python>=4.2.0.34",
        "easydict>=1.9",
        "tqdm>=4.46.0",
        "numpy>=1.18.4"
    ],
    include_package_data=True
)