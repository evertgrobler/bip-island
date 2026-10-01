// swift-tools-version:5.9
// Pure game logic for Bip Island: no AppKit, SpriteKit or audio, so it can be unit-tested anywhere.
import PackageDescription

let package = Package(
    name: "BipCore",
    platforms: [.macOS(.v14)],
    products: [
        .library(name: "BipCore", targets: ["BipCore"]),
    ],
    targets: [
        .target(name: "BipCore"),
        .testTarget(name: "BipCoreTests", dependencies: ["BipCore"]),
    ]
)
