import React, { useRef, useEffect, useState, useCallback } from 'react';

import './App.scss';
import axios from 'axios';
import Grid from '@mui/material/Grid';
import {makeStyles} from '@mui/styles';
import CloudUpload from '@mui/icons-material/CloudUpload'
import { TextareaAutosize ,IconButton, Checkbox, Button, Modal, Typography, Input, CircularProgress, Box } from "@mui/material";

import { ICSRLine, IFile, IMedium, IProgress, ISubtitles } from "./Interfaces";
import Widget from './Components/Widget';


const useStyles = makeStyles({
button: {
    margin: '1px'
},
    overlayContent: {
        background: '#080047',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'rgb(8 0 71 / 50%)',
        marginTop: '110px',
        borderRadius: '50px 50px 0 0',
        height: '50%!important'
    },
  root: {
    padding: '3px',
    paddingRight: 0,
    width: "calc(100% + 1px)",
  },
  row: {
    padding: '1px'
  },
  header: {

    padding: '1px'
  },
  activeRow: {
    border: "1px solid #adb3ba",
    borderRadius: 10,
    padding: '1px',
    backgroundColor: "#e8f2ff",
  },
  columnItemText: {
    fontWeight: "bold",
  },
});
const App = ()=> {
    const [startColor, setStartColor] = useState('red');
    const [videoinput, setVideoInput] = useState<IMedium>({} as IMedium);
    const [location, setLocation] = useState('');
    const [progress, setProgress] = useState(0);
    const [stopColor, setStopColor] = useState('red');
    const inputRef = useRef<HTMLInputElement>(null);
    const [isUploading, setIsUploading] = useState(false);
    const [csrLines, setCsrLines] = useState<ICSRLine[]>([]);
    const [start, setStart] = useState(0.1);
    const [burningQuestion, setBurningQuestion] = useState(false);
    const [textCount, setCount] = useState(1);
    const [lineToDelete, setLineToDelete]  = useState(0);
    const [text, setText] = useState<string[]>([]);
    const [stop, setStop] = useState(2);
    const [showProgress, setShowProgress] = useState(false);
  /** automatically updated whne the video plays */
    const [cur, setCur] = useState(0.0);
  // const dtOptions: Intl.DateTimeFormatOptions = {hour: '2-digit', minute: '2-digit', second: '2-digit', fractionalSecondDigits: 3, hourCycle: 'h24'}
  // const timeFormat = new Intl.DateTimeFormat('en-US', dtOptions);
    useEffect(()=>{
      setCsrLines([{'timeLine': `${secsToTime(start)} --> ${secsToTime(stop)}`, subtitles:['Subtitles were supplied using','Textifator (c)']}])
    } , []);
    const myvideoRef = useRef<HTMLVideoElement>(null);
    const burnSubtitles = () => {
        setBurningQuestion(false);
        const upl = {
            id: videoinput.hashKey + videoinput.ext,
            lines: csrLines.flatMap((m, i) => [(i + 1).toString(), m.timeLine, m.subtitles, ''].flat(1))
        } as ISubtitles;

        axios.post<IFile>(`Movie/Burn`, upl).then((result) => {
            const file = result.data;
            setProgress(0);
            setShowProgress(true);
            const timer = setInterval(() => {
                axios.get<IProgress>(`Movie/Progress/${file.file}`).then(response =>
                {
                    const progress = response.data;
                    setProgress(progress.perc);
                    if (progress.perc === 100) {
                        clearInterval(timer);
                        setShowProgress(false);
                        setLocation(`DownloadedFiles/${file.file}.mp4`);
                        myvideoRef.current?.load();
                        //setMsg('Your subtitles are now in the movie section. You can download teh result. Remember, these files will be removed automatically within a day')
                    }
                });

            }, 2000);
        }
        ).catch((err) => {
            console.error(err.message)
        });


    }
const deleteLine = ()=> {
    const line = lineToDelete -1;
    setCsrLines( csrLines.filter((_f, i) => i !== line))
    setCount(textCount-1);
}

    const handleDrop = (e: React.DragEvent<HTMLElement>) => {
        e.preventDefault();
        e.stopPropagation();
        if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
            doUpload(e.dataTransfer.files[0]);
        }

    }
    const doUpload = (file: File) => {
        setIsUploading(true);
        const formData = new FormData();
        formData.append("formFile", file);
        formData.append("fileName", file.name);

        const config = {
            onUploadProgress: function (progressEvent) {
                const percentCompleted = Math.round((progressEvent.loaded * 100) / progressEvent.total);

                setProgress(percentCompleted);
            }
        };

        axios.post<IMedium>(`/Movie/UploadMedia`, formData, config)
            .then((res) => {
                const loc = res.headers.location;
                setLocation(loc || '');
                setIsUploading(false);
                setVideoInput(res.data);
                myvideoRef.current?.load();

            })
            .catch((err) => {
                console.error(err.message);
            });
    }
//const onDrop = useCallback((acceptedFiles) => {
//  if (acceptedFiles.length < 1) {
//    return;
//  }
//  const file = acceptedFiles[0];
//  const formData = new FormData();
//  formData.append("formFile", file);
//  formData.append("fileName", file.name);
//  setIsUploading(true);
//    /*, {
//        headers: {
//            "Content-Type": "multipart/form-data"
//        }
//    }*/
//    const config = {
//        onUploadProgress: function (progressEvent) {
//            const percentCompleted = Math.round((progressEvent.loaded * 100) / progressEvent.total);
//            console.log("Progress:-" + percentCompleted);
//        }
//    };
//    axios.post<IMedium>(`/Movie/UploadMedia`, formData, config).then((res) => {
//         const loc = res.headers.location;
//         setLocation(loc || '');
//         console.log(`location = ${loc}`)

//         setIsUploading(false);
//        setVideoInput(res.data);
//        myvideoRef.current?.load();

//    })
//    .catch((err) => {
//      console.error(err);
//    });    // Do something with the files
//}, []);

const copy=() => {
  const el = mytext.current;
  if(el) {
    el.select();
    document.execCommand("copy")
  }
}
const Add = () =>{
  const newSTop = stop === 0 ? cur: stop;
  csrLines.push({lineNumber: textCount, timeLine: `${secsToTime(start)} --> ${secsToTime(newSTop)}`, subtitles: text} as ICSRLine);
  setCsrLines(csrLines);
  if (mytext.current){
     mytext.current.scrollTop = mytext.current.scrollHeight;
  }
  setCount(textCount+1);
  setStart(cur); // set new start
  setStop(0);
    setStopColor('red');

    setText([]);
}
  /**formats to HH:mm:ss,ffff */
  const secsToTime = (secs: number): string => {
    // create Date object and set to today's date and time
    const hour = Math.floor(secs/3600 % 24);
    const minute = Math.floor( secs/60 % 60);
    const sec = Math.floor( secs%60);
    const ms = Math.floor((secs % 1) * 1000);
    return `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}:${sec.toString().padStart(2, '0')},${ms.toString().padStart(3, '0')}`;
  }
  const mytext= useRef<HTMLTextAreaElement>(null);
  const classes = useStyles();
    return (

    <Grid className={classes.root}  container>
      <Grid className={classes.header} container xs={12}>
        <Grid item xs={12}>
        <div style={{
      display: 'flex',
      margin: 'auto',
      width: 400,
      flexWrap: 'wrap',
    }}>

        {(showProgress || isUploading) && <CircularProgress color='primary' variant="determinate" value={progress} />}
     {/*<div>{isUploading?<div>Uploading file...</div>:(*/}
     {/*                     <div*/}

     {/*           style={{  minHeight: "200px",width:'500px', height: "100%" }}*/}
     {/*           {...getRootProps()}*/}
     {/*         >*/}
     {/*           <input {...getInputProps()} />*/}
     {/*           {isDragActive ? (*/}
     {/*             <p>Drop the files here ...</p>*/}
     {/*           ) : (*/}
     {/*             <p>Drag 'n' drop some files here, or click to select files</p>*/}
     {/*           )}*/}
     {/*         </div>)}*/}
                        {/*         </div>*/}


                        <Box  alignItems='center' display='flex' justifyContent='center' flexDirection='column'>
                            <Box>
                                <input onChange={(e) => e.target.files && doUpload(e.target.files[0])} ref={inputRef} accept="video/*"type='file' hidden />

                                <IconButton onDrop={handleDrop} onClick={()=>inputRef.current && inputRef.current.click()} color='primary' component="span" id="upload-company-logo" aria-label="Upload" >
                                    <CloudUpload/>
                                </IconButton>


                            </Box>
                        </Box>

      {/* <input accept="image/*" className={classes.input} id="icon-button-file" type="file" />*/}
      {/*<label title="upload your movie to have subtitles" htmlFor="icon-button-file">*/}
      {/*  <IconButton color="primary" className={classes.button} aria-label="upload picture" component="span">*/}
      {/*      <PhotoCamera  onDrop={handleDrop } />*/}
      {/*  </IconButton>*/}
               {/*         </label>*/}

                        <Modal
                            className={classes.overlayContent}
                            style={{ width: 500 }}
                            open={burningQuestion}

                            aria-labelledby="simple-modal-title"
                            aria-describedby="simple-modal-description"
                        >
                                <Widget header="Start burning titles?"  headerAction={null}>
                                Note this will take time! Do not close your browser.
                                A circle will process until 100%.
                                  <br />
                                                            <br />
                                                            <div style={{ width: "100%", textAlign: "center" }}>
                                                                <Button
                                                                    style={{ marginRight: "32px" }}
                                                                    variant="contained"
                                                                    color="secondary"
                                                                    onClick={burnSubtitles}
                                                                >
                                                                    Start
                                    </Button>
                                                                <Button
                                                                    variant="contained"
                                                                    color="primary"
                                                                    onClick={() => setBurningQuestion(false)}>
                                                                    Cancel
                                    </Button>
                                                            </div>
                                                        </Widget>
                        </Modal>

    </div>
        </Grid>
        <Grid item xs={6}>
          <Typography variant="h6" className={classes.columnItemText}>Video</Typography>
        </Grid>
        <Grid item xs={6}>
          <Typography variant="h6" className={classes.columnItemText}>Transcript</Typography>
        </Grid>
      </Grid>
      <Grid  className={classes.row} container spacing={5} xs={12}>
        <Grid item xs={6}>

            <video ref={myvideoRef} controls onSeeked={(e)=>{setStartColor('red'); setStopColor('red');}} onTimeUpdate={(tmr) => setCur(tmr.currentTarget.currentTime)}>
            <source src={location} type={ videoinput.mediaType}/>
          </video>
        </Grid>
        <Grid  item xs={6}>
          <Grid container  xs={12}>
            <Grid  item xs={12}>
              <Button color='primary' style={{backgroundColor:startColor}}  value="Start" onClick={()=>{
              setStart(cur);
              setStartColor('green');}
              } >Start</Button>
                <Button color='primary' style={{backgroundColor:stopColor}} value="Stop" onClick={()=>
                  {
                  if (start> cur) {
                    setCur(cur - 1);
                    }
                    setStopColor('green');
                    setStop(0)}
                  }>Stop</Button>

                <Button color='primary' value="Add" onClick={Add} >ADD</Button>

                <Button  title='Copies current text to clipboard' color='primary' value="Add" onClick={copy} >TO CLIPBOARD</Button>
                <input type='number'  max={textCount} style={{width:'100px'}} onKeyPress={(k)=>{
                  const c = k.code; if (c.startsWith('Digit')) {return}
                   k.preventDefault()}
                  } onChange={(ev) => { setLineToDelete(parseInt(ev.currentTarget.value)); }} />
                          <Button onClick={deleteLine}>Remove </Button>
                          <Button onClick={()=> setBurningQuestion(true)} title="Creates your movie with subtitles">Export</Button>
              </Grid>

              <Grid item xs={12}>
                <TextareaAutosize value={text && text.join('\n')} lang='nl-nl' spellCheck={true} autoCorrect='on' autoComplete='on' autoCapitalize='on'   title='Type your subitles here. Ctrl-Enter to Add and clear' onKeyPress={(k)=>{if (k.ctrlKey && k.code ==='Enter') Add()}}    
                  style={{overflowY:'scroll',  width:'100%', resize: 'none'}}  onChange ={(ev) => setText(ev.currentTarget.value.split('\n'))}/>

              </Grid>
            <Grid item xs={12}>
              <TextareaAutosize ref={mytext} style={{overflowY:'scroll', height:'300px', width: '100%', resize:'none'}}
              readOnly minRows={3}
                value={csrLines.flatMap((m, i) =>[ (i+1).toString(), m.timeLine, m.subtitles, ''].flat(1)).join('\n')} maxRows={20}  />

            </Grid>
           </Grid>
      </Grid>
      </Grid>

    </Grid>

  );
}

export default App;
