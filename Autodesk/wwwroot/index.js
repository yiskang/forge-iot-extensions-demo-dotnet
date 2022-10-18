import 'https://unpkg.com/forge-iot-extensions@0.0.5/dist/index.js';
import { initViewer, loadModel, adjustPanelStyle } from './viewer.js';
import { initTimeline } from './timeline.js';
import { MyDataView } from './dataview.js';
import './sensormanager.js';

const FORGE_MODEL_URN = 'dXJuOmFkc2sub2JqZWN0czpvcy5vYmplY3Q6ZXh0cmFjdC1hdXRvZGVzay1pby0yMDE3bGt3ZWo3eHBiZ3A2M3g0aGwzMzV5Nm0yNm9ha2dnb2YvcmFjX2Jhc2ljX3NhbXBsZV9wcm9qZWN0LnJ2dA';
const FORGE_MODEL_VIEW = 'c884ae1b-61e7-4f9d-0001-719e20b22d0b-0010c0ad';
const FORGE_MODEL_DEFAULT_FLOOR_INDEX = 2;
const DEFAULT_TIMERANGE_START = new Date('2022-01-01');
const DEFAULT_TIMERANGE_END = new Date('2022-01-30');

const IOT_EXTENSION_IDS = ['IoT.SensorList', 'IoT.SensorDetail', 'IoT.SensorSprites', 'IoT.SensorHeatmaps'];
const IOT_PANEL_STYLES = {
    'IoT.SensorList': { right: '10px', top: '10px', width: '500px', height: '300px' },
    'IoT.SensorDetail': { right: '10px', top: '320px', width: '500px', height: '300px' },
    'IoT.SensorHeatmaps': { left: '10px', top: '320px', width: '300px', height: '150px' }
};

let dataView = new MyDataView();
await dataView.init({ start: DEFAULT_TIMERANGE_START, end: DEFAULT_TIMERANGE_END });
let extensions = [];

async function onTimeRangeChanged(start, end) {
    await dataView.refresh({ start, end });
    extensions.forEach(ext => ext.dataView = dataView);
}
function onLevelChanged({ target, levelIndex }) {
    dataView.floor = levelIndex !== undefined ? target.floorData[levelIndex] : null;
    extensions.forEach(ext => ext.dataView = dataView);
}
function onTimeMarkerChanged(time) {
    extensions.forEach(ext => ext.currentTime = time);
}
function onCurrentSensorChanged(sensorId) {
    const sensor = dataView.getSensors().get(sensorId);
    if (sensor && sensor.objectId) {
        viewer.fitToView([sensor.objectId]);
    }
    extensions.forEach(ext => ext.currentSensorID = sensorId);
}
function onCurrentChannelChanged(channelId) {
    extensions.forEach(ext => ext.currentChannelID = channelId);
}

initTimeline(document.getElementById('timeline'), onTimeRangeChanged, onTimeMarkerChanged);
const viewer = await initViewer(document.getElementById('preview'), IOT_EXTENSION_IDS.concat(['Iot.SensorManager', 'Autodesk.AEC.LevelsExtension']));
loadModel(viewer, FORGE_MODEL_URN, FORGE_MODEL_VIEW);
viewer.addEventListener(Autodesk.Viewing.GEOMETRY_LOADED_EVENT, async function () {
    // Setup and auto-activate IoT extensions
    for (const extensionID of IOT_EXTENSION_IDS) {
        const extension = viewer.getExtension(extensionID);
        extensions.push(extension);
        extension.dataView = dataView;
        extension.activate();
        if (IOT_PANEL_STYLES[extensionID]) {
            adjustPanelStyle(extension.panel, IOT_PANEL_STYLES[extensionID]);
        }
    }

    const sensorMgrExt = viewer.getExtension('Iot.SensorManager');
    sensorMgrExt.onSensorAdded = async (data) => {
        await dataView.addSensors(data);

        let timeRange = dataView.getTimerange();
        await dataView.refresh({ start: timeRange[0], end: timeRange[1] });

        extensions.forEach(ext => ext.dataView = dataView);
    };
    sensorMgrExt.onSensorDeleted = async (sensorId) => {
        await dataView.deleteSensors(sensorId);

        let timeRange = dataView.getTimerange();
        await dataView.refresh({ start: timeRange[0], end: timeRange[1] });

        extensions.forEach(ext => {
            ext.dataView = dataView;
            ext.currentSensorID = null;
        });
    };

    // Setup and auto-activate other viewer extensions
    const levelsExt = viewer.getExtension('Autodesk.AEC.LevelsExtension');
    levelsExt.levelsPanel.setVisible(true);
    levelsExt.floorSelector.addEventListener(Autodesk.AEC.FloorSelector.SELECTED_FLOOR_CHANGED, onLevelChanged);
    levelsExt.floorSelector.selectFloor(FORGE_MODEL_DEFAULT_FLOOR_INDEX, true);
    adjustPanelStyle(levelsExt.levelsPanel, { left: '10px', top: '10px', width: '300px', height: '300px' });

    onTimeRangeChanged(DEFAULT_TIMERANGE_START, DEFAULT_TIMERANGE_END);
    viewer.getExtension('IoT.SensorList').onSensorClicked = (sensorId) => onCurrentSensorChanged(sensorId);
    viewer.getExtension('IoT.SensorSprites').onSensorClicked = (sensorId) => onCurrentSensorChanged(sensorId);
    viewer.getExtension('IoT.SensorHeatmaps').onChannelChanged = (channelId) => onCurrentChannelChanged(channelId);
});
