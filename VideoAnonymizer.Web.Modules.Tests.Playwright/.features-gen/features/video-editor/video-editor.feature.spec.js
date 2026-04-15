// Generated from: features\video-editor\video-editor.feature
import { test } from "playwright-bdd";

test.describe('Video editor for analyzed videos', () => {

  test('Editor shows video preview, current-frame object list, and timeline', async ({ Given, Then, And, page }) => { 
    await Given('the editor is opened for an analyzed video', null, { page }); 
    await Then('I should see a video preview', null, { page }); 
    await And('I should see a list of detected objects for the current frame next to the video preview', null, { page }); 
    await And('every detected object in the current frame should have a checkbox', null, { page }); 
    await And('I should see a timeline below the video preview', null, { page }); 
    await And('I should see the detected objects listed on the left side of the timeline', null, { page }); 
    await And('each object on the timeline should have a color', null, { page }); 
    await And('each object on the timeline should have a checkbox', null, { page }); 
  });

  test('Timeline highlights the times where an object is detected', async ({ Given, When, Then, page }) => { 
    await Given('the editor is opened for an analyzed video', null, { page }); 
    await When('an object is detected in one or more analyzed frames', null, { page }); 
    await Then('the timeline should highlight that object in its assigned color at the corresponding times', null, { page }); 
  });

  test('Neighboring detections are rendered as one continuous colored line', async ({ Given, When, Then, And, page }) => { 
    await Given('the editor is opened for an analyzed video', null, { page }); 
    await And('an object is detected in neighboring analyzed frames', null, { page }); 
    await When('the timeline is rendered', null, { page }); 
    await Then('the object should be shown as one continuous colored line across the corresponding time range', null, { page }); 
  });

  test('Bounding box border color matches the timeline object color', async ({ Given, When, Then, page }) => { 
    await Given('the editor is opened for an analyzed video', null, { page }); 
    await When('an object is shown as a bounding box in the video preview', null, { page }); 
    await Then('the border of the bounding box should use the same color as the object on the timeline', null, { page }); 
  });

  test('Current-frame object can be disabled from the object list', async ({ Given, When, Then, And, page }) => { 
    await Given('the editor is opened for an analyzed video', null, { page }); 
    await And('the current frame contains a detected object', null, { page }); 
    await When('I uncheck that object in the current-frame object list', null, { page }); 
    await Then('the object should no longer be active in this frame', null, { page }); 
    await And('the object should not be displayed in the overlay on this frame', null, { page }); 
    await And('the point for the object in this frame should turn gray in the timeline', null, { page }); 
    await And('the other points for the object on the timeline should not be effected', null, { page }); 
  });

  test('Current-frame object can be enabled from the object list', async ({ Given, When, Then, And, page }) => { 
    await Given('the editor is opened for an analyzed video', null, { page }); 
    await And('the current frame contains a detected object', null, { page }); 
    await And('one object is disabled in the object list', null, { page }); 
    await When('I check that object in the current-frame object list', null, { page }); 
    await Then('the object should be active in this frame', null, { page }); 
    await And('the object should be displayed in the overlay on this frame', null, { page }); 
    await And('the point for the object in this frame should turn to its color in the timeline', null, { page }); 
    await And('the other points for the object on the timeline should not be effected', null, { page }); 
  });

  test('Timeline object can be disabled from the timeline list', async ({ Given, When, Then, And, page }) => { 
    await Given('the editor is opened for an analyzed video', null, { page }); 
    await And('the timeline contains a detected object', null, { page }); 
    await When('I uncheck that object in the timeline list', null, { page }); 
    await Then('the object should no longer be selected in any frame', null, { page }); 
    await And('the line or points in the timeline for the object should turn gray', null, { page }); 
  });

  test('Timeline object can be enabled from the timeline list', async ({ Given, When, Then, And, page }) => { 
    await Given('the editor is opened for an analyzed video', null, { page }); 
    await And('the timeline contains a detected object', null, { page }); 
    await And('one object has been disabled on the timeline', null, { page }); 
    await When('I check that object in the timeline list', null, { page }); 
    await Then('the object should be selected in all frames', null, { page }); 
    await And('the line or points in the timeline for the object should turn to its color', null, { page }); 
  });

  test('Playback indicator moves with the current video time', async ({ Given, When, Then, And, page }) => { 
    await Given('the editor is opened for an analyzed video', null, { page }); 
    await When('the video is playing', null, { page }); 
    await Then('a vertical playback indicator should move on the timeline with the current video time', null, { page }); 
    await And('the current timestamp should be shown at the indicator', null, { page }); 
  });

  test('Clicking on the timeline updates the video current time', async ({ Given, When, Then, And, page }) => { 
    await Given('the editor is opened for an analyzed video', null, { page }); 
    await And('the video is paused', null, { page }); 
    await When('I click on a position on the timeline at 3 seconds', null, { page }); 
    await Then('the video current time should be set to 3 seconds', null, { page }); 
    await And('the video frame displayed should correspond to 3 seconds', null, { page }); 
    await And('the playback indicator should move to the clicked position on the timeline', null, { page }); 
  });

});

// == technical section ==

test.use({
  $test: [({}, use) => use(test), { scope: 'test', box: true }],
  $uri: [({}, use) => use('features\\video-editor\\video-editor.feature'), { scope: 'test', box: true }],
  $bddFileData: [({}, use) => use(bddFileData), { scope: "test", box: true }],
});

const bddFileData = [ // bdd-data-start
  {"pwTestLine":6,"pickleLine":7,"tags":[],"steps":[{"pwStepLine":7,"gherkinStepLine":8,"keywordType":"Context","textWithKeyword":"Given the editor is opened for an analyzed video","stepMatchArguments":[]},{"pwStepLine":8,"gherkinStepLine":9,"keywordType":"Outcome","textWithKeyword":"Then I should see a video preview","stepMatchArguments":[]},{"pwStepLine":9,"gherkinStepLine":10,"keywordType":"Outcome","textWithKeyword":"And I should see a list of detected objects for the current frame next to the video preview","stepMatchArguments":[]},{"pwStepLine":10,"gherkinStepLine":11,"keywordType":"Outcome","textWithKeyword":"And every detected object in the current frame should have a checkbox","stepMatchArguments":[]},{"pwStepLine":11,"gherkinStepLine":12,"keywordType":"Outcome","textWithKeyword":"And I should see a timeline below the video preview","stepMatchArguments":[]},{"pwStepLine":12,"gherkinStepLine":13,"keywordType":"Outcome","textWithKeyword":"And I should see the detected objects listed on the left side of the timeline","stepMatchArguments":[]},{"pwStepLine":13,"gherkinStepLine":14,"keywordType":"Outcome","textWithKeyword":"And each object on the timeline should have a color","stepMatchArguments":[]},{"pwStepLine":14,"gherkinStepLine":15,"keywordType":"Outcome","textWithKeyword":"And each object on the timeline should have a checkbox","stepMatchArguments":[]}]},
  {"pwTestLine":17,"pickleLine":17,"tags":[],"steps":[{"pwStepLine":18,"gherkinStepLine":18,"keywordType":"Context","textWithKeyword":"Given the editor is opened for an analyzed video","stepMatchArguments":[]},{"pwStepLine":19,"gherkinStepLine":19,"keywordType":"Action","textWithKeyword":"When an object is detected in one or more analyzed frames","stepMatchArguments":[]},{"pwStepLine":20,"gherkinStepLine":20,"keywordType":"Outcome","textWithKeyword":"Then the timeline should highlight that object in its assigned color at the corresponding times","stepMatchArguments":[]}]},
  {"pwTestLine":23,"pickleLine":22,"tags":[],"steps":[{"pwStepLine":24,"gherkinStepLine":23,"keywordType":"Context","textWithKeyword":"Given the editor is opened for an analyzed video","stepMatchArguments":[]},{"pwStepLine":25,"gherkinStepLine":24,"keywordType":"Context","textWithKeyword":"And an object is detected in neighboring analyzed frames","stepMatchArguments":[]},{"pwStepLine":26,"gherkinStepLine":25,"keywordType":"Action","textWithKeyword":"When the timeline is rendered","stepMatchArguments":[]},{"pwStepLine":27,"gherkinStepLine":26,"keywordType":"Outcome","textWithKeyword":"Then the object should be shown as one continuous colored line across the corresponding time range","stepMatchArguments":[]}]},
  {"pwTestLine":30,"pickleLine":29,"tags":[],"steps":[{"pwStepLine":31,"gherkinStepLine":30,"keywordType":"Context","textWithKeyword":"Given the editor is opened for an analyzed video","stepMatchArguments":[]},{"pwStepLine":32,"gherkinStepLine":31,"keywordType":"Action","textWithKeyword":"When an object is shown as a bounding box in the video preview","stepMatchArguments":[]},{"pwStepLine":33,"gherkinStepLine":32,"keywordType":"Outcome","textWithKeyword":"Then the border of the bounding box should use the same color as the object on the timeline","stepMatchArguments":[]}]},
  {"pwTestLine":36,"pickleLine":34,"tags":[],"steps":[{"pwStepLine":37,"gherkinStepLine":35,"keywordType":"Context","textWithKeyword":"Given the editor is opened for an analyzed video","stepMatchArguments":[]},{"pwStepLine":38,"gherkinStepLine":36,"keywordType":"Context","textWithKeyword":"And the current frame contains a detected object","stepMatchArguments":[]},{"pwStepLine":39,"gherkinStepLine":37,"keywordType":"Action","textWithKeyword":"When I uncheck that object in the current-frame object list","stepMatchArguments":[]},{"pwStepLine":40,"gherkinStepLine":38,"keywordType":"Outcome","textWithKeyword":"Then the object should no longer be active in this frame","stepMatchArguments":[]},{"pwStepLine":41,"gherkinStepLine":39,"keywordType":"Outcome","textWithKeyword":"And the object should not be displayed in the overlay on this frame","stepMatchArguments":[]},{"pwStepLine":42,"gherkinStepLine":40,"keywordType":"Outcome","textWithKeyword":"And the point for the object in this frame should turn gray in the timeline","stepMatchArguments":[]},{"pwStepLine":43,"gherkinStepLine":41,"keywordType":"Outcome","textWithKeyword":"And the other points for the object on the timeline should not be effected","stepMatchArguments":[]}]},
  {"pwTestLine":46,"pickleLine":43,"tags":[],"steps":[{"pwStepLine":47,"gherkinStepLine":44,"keywordType":"Context","textWithKeyword":"Given the editor is opened for an analyzed video","stepMatchArguments":[]},{"pwStepLine":48,"gherkinStepLine":45,"keywordType":"Context","textWithKeyword":"And the current frame contains a detected object","stepMatchArguments":[]},{"pwStepLine":49,"gherkinStepLine":46,"keywordType":"Context","textWithKeyword":"And one object is disabled in the object list","stepMatchArguments":[]},{"pwStepLine":50,"gherkinStepLine":47,"keywordType":"Action","textWithKeyword":"When I check that object in the current-frame object list","stepMatchArguments":[]},{"pwStepLine":51,"gherkinStepLine":48,"keywordType":"Outcome","textWithKeyword":"Then the object should be active in this frame","stepMatchArguments":[]},{"pwStepLine":52,"gherkinStepLine":49,"keywordType":"Outcome","textWithKeyword":"And the object should be displayed in the overlay on this frame","stepMatchArguments":[]},{"pwStepLine":53,"gherkinStepLine":50,"keywordType":"Outcome","textWithKeyword":"And the point for the object in this frame should turn to its color in the timeline","stepMatchArguments":[]},{"pwStepLine":54,"gherkinStepLine":51,"keywordType":"Outcome","textWithKeyword":"And the other points for the object on the timeline should not be effected","stepMatchArguments":[]}]},
  {"pwTestLine":57,"pickleLine":53,"tags":[],"steps":[{"pwStepLine":58,"gherkinStepLine":54,"keywordType":"Context","textWithKeyword":"Given the editor is opened for an analyzed video","stepMatchArguments":[]},{"pwStepLine":59,"gherkinStepLine":55,"keywordType":"Context","textWithKeyword":"And the timeline contains a detected object","stepMatchArguments":[]},{"pwStepLine":60,"gherkinStepLine":56,"keywordType":"Action","textWithKeyword":"When I uncheck that object in the timeline list","stepMatchArguments":[]},{"pwStepLine":61,"gherkinStepLine":57,"keywordType":"Outcome","textWithKeyword":"Then the object should no longer be selected in any frame","stepMatchArguments":[]},{"pwStepLine":62,"gherkinStepLine":58,"keywordType":"Outcome","textWithKeyword":"And the line or points in the timeline for the object should turn gray","stepMatchArguments":[]}]},
  {"pwTestLine":65,"pickleLine":60,"tags":[],"steps":[{"pwStepLine":66,"gherkinStepLine":61,"keywordType":"Context","textWithKeyword":"Given the editor is opened for an analyzed video","stepMatchArguments":[]},{"pwStepLine":67,"gherkinStepLine":62,"keywordType":"Context","textWithKeyword":"And the timeline contains a detected object","stepMatchArguments":[]},{"pwStepLine":68,"gherkinStepLine":63,"keywordType":"Context","textWithKeyword":"And one object has been disabled on the timeline","stepMatchArguments":[]},{"pwStepLine":69,"gherkinStepLine":64,"keywordType":"Action","textWithKeyword":"When I check that object in the timeline list","stepMatchArguments":[]},{"pwStepLine":70,"gherkinStepLine":65,"keywordType":"Outcome","textWithKeyword":"Then the object should be selected in all frames","stepMatchArguments":[]},{"pwStepLine":71,"gherkinStepLine":66,"keywordType":"Outcome","textWithKeyword":"And the line or points in the timeline for the object should turn to its color","stepMatchArguments":[]}]},
  {"pwTestLine":74,"pickleLine":68,"tags":[],"steps":[{"pwStepLine":75,"gherkinStepLine":69,"keywordType":"Context","textWithKeyword":"Given the editor is opened for an analyzed video","stepMatchArguments":[]},{"pwStepLine":76,"gherkinStepLine":70,"keywordType":"Action","textWithKeyword":"When the video is playing","stepMatchArguments":[]},{"pwStepLine":77,"gherkinStepLine":71,"keywordType":"Outcome","textWithKeyword":"Then a vertical playback indicator should move on the timeline with the current video time","stepMatchArguments":[]},{"pwStepLine":78,"gherkinStepLine":72,"keywordType":"Outcome","textWithKeyword":"And the current timestamp should be shown at the indicator","stepMatchArguments":[]}]},
  {"pwTestLine":81,"pickleLine":74,"tags":[],"steps":[{"pwStepLine":82,"gherkinStepLine":75,"keywordType":"Context","textWithKeyword":"Given the editor is opened for an analyzed video","stepMatchArguments":[]},{"pwStepLine":83,"gherkinStepLine":76,"keywordType":"Context","textWithKeyword":"And the video is paused","stepMatchArguments":[]},{"pwStepLine":84,"gherkinStepLine":77,"keywordType":"Action","textWithKeyword":"When I click on a position on the timeline at 3 seconds","stepMatchArguments":[{"group":{"start":41,"value":"3","children":[]},"parameterTypeName":"int"}]},{"pwStepLine":85,"gherkinStepLine":78,"keywordType":"Outcome","textWithKeyword":"Then the video current time should be set to 3 seconds","stepMatchArguments":[{"group":{"start":40,"value":"3","children":[]},"parameterTypeName":"int"}]},{"pwStepLine":86,"gherkinStepLine":79,"keywordType":"Outcome","textWithKeyword":"And the video frame displayed should correspond to 3 seconds","stepMatchArguments":[{"group":{"start":47,"value":"3","children":[]},"parameterTypeName":"int"}]},{"pwStepLine":87,"gherkinStepLine":80,"keywordType":"Outcome","textWithKeyword":"And the playback indicator should move to the clicked position on the timeline","stepMatchArguments":[]}]},
]; // bdd-data-end