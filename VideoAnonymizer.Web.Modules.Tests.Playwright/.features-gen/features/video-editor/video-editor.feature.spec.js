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

});

// == technical section ==

test.use({
  $test: [({}, use) => use(test), { scope: 'test', box: true }],
  $uri: [({}, use) => use('features\\video-editor\\video-editor.feature'), { scope: 'test', box: true }],
  $bddFileData: [({}, use) => use(bddFileData), { scope: "test", box: true }],
});

const bddFileData = [ // bdd-data-start
  {"pwTestLine":6,"pickleLine":7,"tags":[],"steps":[{"pwStepLine":7,"gherkinStepLine":8,"keywordType":"Context","textWithKeyword":"Given the editor is opened for an analyzed video","stepMatchArguments":[]},{"pwStepLine":8,"gherkinStepLine":9,"keywordType":"Outcome","textWithKeyword":"Then I should see a video preview","stepMatchArguments":[]},{"pwStepLine":9,"gherkinStepLine":10,"keywordType":"Outcome","textWithKeyword":"And I should see a list of detected objects for the current frame next to the video preview","stepMatchArguments":[]},{"pwStepLine":10,"gherkinStepLine":11,"keywordType":"Outcome","textWithKeyword":"And every detected object in the current frame should have a checkbox","stepMatchArguments":[]},{"pwStepLine":11,"gherkinStepLine":12,"keywordType":"Outcome","textWithKeyword":"And I should see a timeline below the video preview","stepMatchArguments":[]},{"pwStepLine":12,"gherkinStepLine":13,"keywordType":"Outcome","textWithKeyword":"And I should see the detected objects listed on the left side of the timeline","stepMatchArguments":[]},{"pwStepLine":13,"gherkinStepLine":14,"keywordType":"Outcome","textWithKeyword":"And each object on the timeline should have a color","stepMatchArguments":[]},{"pwStepLine":14,"gherkinStepLine":15,"keywordType":"Outcome","textWithKeyword":"And each object on the timeline should have a checkbox","stepMatchArguments":[]}]},
]; // bdd-data-end