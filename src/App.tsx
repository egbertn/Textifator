import React, { useRef, useEffect, useState } from "react";

import './App.scss';
import Grid from '@material-ui/core/Grid';
import { TextareaAutosize , Checkbox, Button, Modal, makeStyles, Typography, Input } from "@material-ui/core";
import logo from './logo.svg';
interface ICSRLine{
  /**e.g.00:00:00,000 --> 00:00:10,000 */ 
  timeLine: string;
  subtitles: string[]
}

const useStyles = makeStyles((theme) => ({
button: {
    margin: theme.spacing(1),
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
    padding: theme.spacing(3),
    paddingRight: 0,
    width: "calc(100% + 1px)",
  },
  row: {
    padding: theme.spacing(1),
  },
  header: {

    padding: theme.spacing(1),
  },
  activeRow: {
    border: "1px solid #adb3ba",
    borderRadius: 10,
    padding: theme.spacing(1),
    backgroundColor: "#e8f2ff",
  },
  columnItemText: {
    fontWeight: "bold",
  },
}));
const App = ()=> {
  const [startColor, setStartColor] = useState('red');
  const [stopColor, setStopColor] = useState('red');
  const [csrLines, setCsrLines] = useState<ICSRLine[]>([]);
  const [start, setStart] = useState(0.1);
  const [textCount, setCount] = useState(1);
  const [lineToDelete, setLineToDelete]  = useState(0);
  const [text, setText] = useState<string[]>([]);
  const [stop, setStop] = useState(2);
  /** automatically updated whne the video plays */
  const [cur, setCur] = useState(0.0);
  // const dtOptions: Intl.DateTimeFormatOptions = {hour: '2-digit', minute: '2-digit', second: '2-digit', fractionalSecondDigits: 3, hourCycle: 'h24'}
  // const timeFormat = new Intl.DateTimeFormat('en-US', dtOptions);
  	useEffect(()=>{
      setCsrLines([{'timeLine': `${secsToTime(start)} --> ${secsToTime(stop)}`, subtitles:['Subtitles were supplied using','Textifator (c)']}])
    } , []);
const deleteLine = ()=> {
    const line = lineToDelete -1;
    setCsrLines( csrLines.filter((_f, i) => i !== line))
    setCount(textCount-1);
}
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
  if (mytextType.current){  
    mytextType.current.value = "";
  }
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
  const mytextType = useRef<HTMLTextAreaElement>(null);
  const classes = useStyles();
  return (
    <Grid className={classes.root}  container>
      <Grid className={classes.header} container xs={12}>
        <Grid item xs={6}>
          <Typography variant="h6" className={classes.columnItemText}>Video</Typography>
        </Grid>
        <Grid item xs={6}>
          <Typography variant="h6" className={classes.columnItemText}>Transcript</Typography>
        </Grid>
      </Grid>
      <Grid  className={classes.row} container spacing={5} xs={12}>
        <Grid item xs={6}>

          <video  controls onSeeked={(e)=>{setStartColor('red'); setStopColor('red');}} onTimeUpdate={(tmr) => setCur(tmr.currentTarget.currentTime)}>
            <source src='myvideo.mp4' type='video/mp4'/>
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
                  } onChange={(ev)=>{setLineToDelete(parseInt( ev.currentTarget.value));}} /> <Button onClick={deleteLine}>Remove </Button>
              </Grid>

              <Grid item xs={12}>
                <TextareaAutosize lang='nl-nl' spellCheck={true} autoCorrect='on' autoComplete='on' autoCapitalize='on'  ref={mytextType} title='Type your subitles here. Ctrl-Enter to Add and clear' onKeyPress={(k)=>{if (k.ctrlKey && k.code ==='Enter') Add()}}    rows={2} style={{overflowY:'scroll',  width:'100%', resize: 'none'}}  onChange ={(ev) => setText(ev.currentTarget.value.split('\n'))}/>

              </Grid>
            <Grid item xs={12}>
              <TextareaAutosize ref={mytext} style={{overflowY:'scroll', height:'300px', width: '100%', resize:'none'}}
              readOnly rowsMin={3} 
                value={csrLines.flatMap((m, i) =>[ i+1, m.timeLine, m.subtitles, ''].flat(1)).join('\n')} rowsMax={20}  />
                
            </Grid>
           </Grid>
      </Grid>
      </Grid>
      
    </Grid>
    
  );
}

export default App;
