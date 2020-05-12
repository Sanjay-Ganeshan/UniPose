import setuptools

with open("README.md", "r") as fh:
    long_description = fh.read()

setuptools.setup(
    name="unipose", # Replace with your own username
    version="1.0.1",
    author="Julie Ganeshan",
    author_email="HeavenlyQueen@outlook.com",
    description="2D Pose in Python",
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
        "torch",
        "torchvision",
        "opencv-python",
        "easydict",
        "tqdm",
        "numpy"
    ],
    include_package_data=True
)