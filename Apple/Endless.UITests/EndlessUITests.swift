import XCTest

final class EndlessUITests: XCTestCase {
    let app = XCUIApplication(bundleIdentifier: "dev.endlessapple.client")
    override func setUpWithError() throws { continueAfterFailure = false }
    override func tearDownWithError() throws { app.terminate() }

    func testLoginInput() {
        app.launchArguments = ["--keyboard-preview"]
        app.launch()
        let username = app.textFields.firstMatch
        XCTAssertTrue(username.waitForExistence(timeout: 20))
        username.tap()
        username.typeText("synthetic")
        let password = app.secureTextFields.firstMatch
        XCTAssertTrue(password.exists)
        password.tap()
        password.typeText("test-only")
        XCTAssertTrue(password.isHittable)
        XCTAssertEqual((password.value as? String ?? "").count, 9)
        XCTAssertTrue(app.buttons["remember-account"].exists)
        XCTAssertTrue(app.buttons["Sign in"].exists)
        XCTAssertFalse(app.buttons["check-server"].exists)
        XCTAssertFalse(app.buttons["forget-account"].exists)
        let screenshot = XCTAttachment(screenshot: XCUIScreen.main.screenshot())
        screenshot.name = "Login keyboard and synthetic input"
        screenshot.lifetime = .keepAlways
        add(screenshot)
        // Never submit synthetic credentials or change real remembered-account settings.
    }

    func testInventorySelectionAndStableViewport() {
        app.launchArguments = ["--hud-preview", "--expanded"]
        app.launch()
        let item = app.buttons["inventory-item-0"]
        XCTAssertTrue(item.waitForExistence(timeout: 20))
        let world = app.images["game-world"]
        let worldFrame = world.frame
        let attackFrame = app.buttons["Attack"].frame
        item.tap()
        XCTAssertNotEqual(app.staticTexts["inventory-selection"].label, "Select an item")
        app.scrollViews["inventory-grid"].swipeUp()
        XCTAssertEqual(world.frame, worldFrame)
        XCTAssertEqual(app.buttons["Attack"].frame, attackFrame)
        let screenshot = XCTAttachment(screenshot: XCUIScreen.main.screenshot())
        screenshot.name = "Inventory original item footprints"
        screenshot.lifetime = .keepAlways
        add(screenshot)
        XCTAssertEqual(app.buttons["hud-bag"].value as? String,"Open")
        app.buttons["hud-bag"].tap()
        XCTAssertEqual(app.buttons["hud-bag"].value as? String,"Closed")
        XCTAssertEqual(world.frame, worldFrame)
        XCTAssertEqual(app.buttons["Attack"].frame, attackFrame)
        app.buttons["hud-stats"].tap()
        XCTAssertEqual(app.staticTexts["stat-hp"].label,"HP: 100 / 100")
        XCTAssertEqual(app.staticTexts["stat-stamina"].label,"Stamina unavailable")
        let stats = XCTAttachment(screenshot: XCUIScreen.main.screenshot())
        stats.name = "Server-derived stats and unknown fields"
        stats.lifetime = .keepAlways
        add(stats)
        app.buttons["hud-chat"].tap()
        XCTAssertEqual(app.buttons["hud-chat"].value as? String,"Open")
        XCTAssertEqual(app.buttons["hud-stats"].value as? String,"Closed")
        app.buttons["hud-chat"].tap()
        XCTAssertEqual(app.buttons["hud-chat"].value as? String,"Closed")
        app.buttons["hud-players"].tap()
        XCTAssertTrue(app.textViews["nearby-players"].exists)
        app.buttons["hud-paperdoll"].tap()
        XCTAssertTrue(app.textViews["paperdoll-equipment"].exists)
        XCTAssertEqual(app.buttons["hud-players"].value as? String,"Closed")
        let doll = XCTAttachment(screenshot:XCUIScreen.main.screenshot())
        doll.name="Paperdoll and selected control"
        doll.lifetime = .keepAlways
        add(doll)
        XCTAssertEqual(world.frame, worldFrame)
        XCTAssertEqual(app.buttons["Attack"].frame, attackFrame)
    }

    func testProgressAboveCredentials() {
        app.launchArguments=["--login-preview","--status-preview"]
        app.launch()
        let field=app.textFields.firstMatch
        XCTAssertTrue(field.waitForExistence(timeout:20))
        XCTAssertLessThanOrEqual(app.staticTexts["server-status"].frame.maxY,field.frame.minY)
        XCTAssertGreaterThan(app.images["login-splash"].frame.maxY,app.frame.maxY-8)
        let image=XCTAttachment(screenshot:XCUIScreen.main.screenshot())
        image.name="Login progress above form and bottom artwork overlap";image.lifetime = .keepAlways;add(image)
    }

    func testZoomAndReorganizedControls() {
        app.launchArguments=["--hud-preview"]
        app.launch()
        let world=app.images["game-world"]
        XCTAssertTrue(world.waitForExistence(timeout:20))
        let attack=app.buttons["Attack"]
        let attackFrame=attack.frame
        let worldFrame=world.frame
        world.pinch(withScale:2,velocity:1)
        XCTAssertTrue((world.value as? String ?? "").contains("Zoom"))
        XCTAssertEqual(world.frame,worldFrame)
        XCTAssertEqual(attack.frame,attackFrame)
        let zoom=XCTAttachment(screenshot:XCUIScreen.main.screenshot())
        zoom.name="Pinch zoom and three-column controls";zoom.lifetime = .keepAlways;add(zoom)
        app.buttons["hud-quests"].tap()
        XCTAssertEqual(app.buttons["hud-quests"].value as? String,"Open")
        app.buttons["hud-quests"].tap()
        XCTAssertEqual(app.buttons["hud-quests"].value as? String,"Closed")
        let before=attack.value as? String
        app.buttons["Move northwest"].press(forDuration:0.5)
        XCTAssertTrue(attack.isEnabled)
        XCTAssertEqual(attack.value as? String,before)
        XCTAssertFalse(app.buttons["Use"].exists)
    }

    func testMapPanAndAttackRecenter() {
        app.launchArguments=["--hud-preview"]
        app.launch()
        let world=app.images["game-world"]
        XCTAssertTrue(world.waitForExistence(timeout:20))
        let frame=world.frame
        let attack=app.buttons["Attack"]
        let attackFrame=attack.frame
        let centeredPixels=world.screenshot().pngRepresentation
        let start=world.coordinate(withNormalizedOffset:CGVector(dx:0.35,dy:0.5))
        let end=world.coordinate(withNormalizedOffset:CGVector(dx:0.7,dy:0.6))
        start.press(forDuration:0.05,thenDragTo:end)
        XCTAssertTrue((world.value as? String ?? "").contains("panned"))
        XCTAssertEqual(world.frame,frame)
        XCTAssertEqual(attack.frame,attackFrame)
        XCTAssertNotEqual(world.screenshot().pngRepresentation,centeredPixels)
        let panned=XCTAttachment(screenshot:XCUIScreen.main.screenshot())
        panned.name="Dragged map without moving HUD";panned.lifetime = .keepAlways;add(panned)
        app.buttons["hud-paperdoll"].tap()
        XCTAssertTrue((world.value as? String ?? "").contains("panned"))
        app.buttons["hud-paperdoll"].tap()
        attack.tap()
        XCTAssertTrue((world.value as? String ?? "").contains("centered"))
        let capture=XCTAttachment(screenshot:XCUIScreen.main.screenshot())
        capture.name="Camera recentered and refreshed buttons";capture.lifetime = .keepAlways;add(capture)
    }

    func testHeldAttackStopsOnRelease() {
        app.launchArguments = ["--hud-preview"]
        app.launch()
        let attack = app.buttons["Attack"]
        XCTAssertTrue(attack.waitForExistence(timeout:20))
        attack.press(forDuration:1.6)
        let count = Int(attack.value as? String ?? "") ?? 0
        XCTAssertGreaterThanOrEqual(count, 2)
        XCTAssertLessThanOrEqual(count, 4)
        Thread.sleep(forTimeInterval:0.8)
        XCTAssertEqual(Int(attack.value as? String ?? ""),count)
    }

    func testSoftwareKeyboardLayout() throws {
        app.launchArguments = ["--keyboard-preview"]
        app.launch()
        let field=app.textFields.firstMatch
        XCTAssertTrue(field.waitForExistence(timeout:20))
        field.tap()
        let keyboard=app.keyboards.firstMatch
        guard keyboard.exists && keyboard.frame.intersection(app.frame).height>100 else {
            throw XCTSkip("This simulator exposes no visible software keyboard; physical-phone keyboard/AutoFill review remains required.")
        }
        field.typeText("synthetic\n")
        XCTAssertTrue(app.secureTextFields.firstMatch.isHittable)
        let screenshot=XCTAttachment(screenshot:XCUIScreen.main.screenshot())
        screenshot.name="Software keyboard layout"
        screenshot.lifetime = .keepAlways
        add(screenshot)
    }

    func testCharacterNavigation() {
        app.launchArguments = ["--selection-preview"]
        app.launch()
        let next = app.buttons["Next character"]
        XCTAssertTrue(next.waitForExistence(timeout: 20))
        XCTAssertTrue(app.staticTexts["1 / 3"].exists)
        next.tap()
        XCTAssertTrue(app.staticTexts["2 / 3"].waitForExistence(timeout: 5))
        app.buttons["Previous character"].tap()
        XCTAssertTrue(app.staticTexts["1 / 3"].waitForExistence(timeout: 5))
        XCTAssertFalse(app.buttons["Delete"].exists)
        XCTAssertGreaterThanOrEqual(app.images["login-splash"].frame.minY, app.buttons["leave-selection"].frame.maxY)
        let screenshot = XCTAttachment(screenshot: XCUIScreen.main.screenshot())
        screenshot.name = "Character navigation"
        screenshot.lifetime = .keepAlways
        add(screenshot)
    }
}
