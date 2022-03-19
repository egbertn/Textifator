import React from "react";
import {Card, Grid, IconButton, Button, Typography} from "@mui/material";


import { makeStyles } from "@mui/styles";


const useStyles = makeStyles({
        root: {
            padding: '3px',
        },
        children: {
            paddingTop: '3px',
        },
    });
const Widget = ({ header, headerAction, children }) => {
    const classes = useStyles();
    return (
        <Card className={classes.root}>
            <Grid container>
                <Grid container item justifyContent="space-between">
                    <Typography variant="h5">{header}</Typography>
                    {headerAction ? (
                        headerAction.icon ? (
                            <IconButton onClick={headerAction ? headerAction.action : null}><div>{headerAction.icon}</div></IconButton>
                        ) : (

                                <Button
                                    size="small"
                                    color="primary"
                                    onClick={headerAction.action}
                                    variant="contained"
                                >
                                    {headerAction.text}
                                </Button>
                            )
                    ) : null}
                </Grid>
                <Grid xs={12} container item className={classes.children}>
                    {children}
                </Grid>
            </Grid>
        </Card>
    );
};

export default Widget;