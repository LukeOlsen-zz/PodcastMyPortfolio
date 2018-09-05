import React, { Component } from 'react';
import { Alert, Button, Card, CardHeader, CardFooter, CardBody, Col, Form, FormFeedback, FormText, FormGroup, Label, Input, Modal, ModalBody, ModalFooter, ModalHeader } from 'reactstrap';
import axios from 'axios';
import { authHeader } from '../../_authHeader';
import { Formik } from 'formik';
import * as Yup from 'yup';
import '../../ValidationForms.css';
import { Switch, Redirect, Route } from 'react-router';
import { BrowserRouter, Link, withRouter } from 'react-router-dom';
import DatePicker from 'react-datepicker';
import moment from 'moment';
import 'react-datepicker/dist/react-datepicker.css';
import { isNullOrUndefined, error } from 'util';

const validationSchema = function (values) {
  return Yup.object().shape({
    title: Yup.string()
      .required('Title is required'),
    description: Yup.string()
      .required('Description is required'),
    startson: Yup.date()
      .required('Starting date and time is required')
  });
};

const validate = (getValidationSchema) => {
  return (values) => {
    const validationSchema = getValidationSchema(values);
    try {
      validationSchema.validateSync(values, { abortEarly: false });
      return {};
    } catch (error) {
      return getErrorsFromValidationError(error);
    }
  };
};

const getErrorsFromValidationError = (validationError) => {
  const FIRST_ERROR = 0;
  return validationError.inner.reduce((errors, error) => {
    return {
      ...errors,
      [error.path]: error.errors[FIRST_ERROR]
    };
  }, {});
};

const onSubmit = (values, { setSubmitting, setErrors, setFieldError }) => {
  if (isNullOrUndefined(values.audiofile) || values.audiofile.type === 'audio/mpeg') {
    const fd = new FormData();

    if (!isNullOrUndefined(values.audiofile)) {
      fd.append("file", values.audiofile, 'firmpodcastsegment.mp3');
    }
    fd.append("title", values.title);
    fd.append("description", values.description);
    fd.append("comment", values.comment);
    fd.append("startsOn", values.startson.format('MM-DD-YYYY h:mm:00'));
    fd.append("endsOn", values.endson.format('MM-DD-YYYY h:mm:00'));

    if (values.id === 0) {
      axios.post('/api/firmpodcastsegments/create', fd, {
        headers: { ...authHeader() }
      })
        .then(function (response) {
          setTimeout(() => {
            // At this point save was successful (get id of newly created segment)
            values.id = response.data.id;
            values.updateNoticeVisible = true;
            values.updateNoticeMessage = 'Firm podcast segment created';
            values.updateNoticeStyle = 'success';
            setSubmitting(false);
          }, 1000);
        })
        .catch(function (error) {
          console.log(error);
          if (error.response.status === 409) {
            sessionStorage.removeItem('token');
            window.location.replace('/login');
          }
          if (error.response.status === 500) {
            // Display error for user
            if (!isNullOrUndefined(error.response.data.field)) {
              setFieldError(error.response.data.field, error.response.data.message);
            }
            values.updateNoticeVisible = true;
            values.updateNoticeMessage = 'Firm podcast segment not created';
            values.updateNoticeStyle = 'danger';
            setSubmitting(false);
          }
        });
    }
    else {
      fd.append("id", values.id);
      axios.put('/api/firmpodcastsegments/update', fd, {
        headers: { ...authHeader() }
      })
        .then(function (response) {
          setTimeout(() => {
            // At this point save was successful
            values.updateNoticeVisible = true;
            values.updateNoticeMessage = 'Firm podcast segment updated';
            values.updateNoticeStyle = 'success';
            setSubmitting(false);
          }, 1000);
        })
        .catch(function (error) {
          console.log(error);
          if (error.response.status === 409) {
            sessionStorage.removeItem('token');
            window.location.replace('/login');
          }
          if (error.response.status === 500) {
            // Display error for user
            if (!isNullOrUndefined(error.response.data.field)) {
              setFieldError(error.response.data.field, error.response.data.message);
            }
            values.updateNoticeVisible = true;
            values.updateNoticeMessage = 'Firm podcast segment not updated';
            values.updateNoticeStyle = 'danger';
            setSubmitting(false);
          }
        });
    }
  }
  else {
    setFieldError('audiofile', 'Audio must be in a mp3 format');
    setSubmitting(false);
  }
};

class FirmPodcastSegment extends Component {
  constructor(props) {
    super(props);
    this.state = {
      id: 0,
      title: '',
      description: '',
      comment: '',
      segmentid: '',
      startson: moment(),
      endson: moment(),
      audiofile: null,
      deleteModal: false
    };

    this.touchAll = this.touchAll.bind(this);
    this.toggleDeleteModal = this.toggleDeleteModal.bind(this);
    this.onDelete = this.onDelete.bind(this);
  }

  componentDidMount() {
    const { match: { params } } = this.props;

    if (params.id !== '0') {
      axios.get(`/api/firmpodcastsegments/${params.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
        .then(response => {
          if (response.status === 200) {
            this.setState({ id: response.data.id });
            this.setState({ title: response.data.title });
            this.setState({ description: response.data.description });
            this.setState({ comment: response.data.comment });
            this.setState({ segmentid: response.data.segmentId });
            this.setState({ segmenturl: response.data.segmenturl });
            this.setState({ startson: moment(response.data.startsOn) });
            this.setState({ endson: moment(response.data.endsOn) });

            document.getElementById("deleteLink").style.visibility = "visible";
          }
        })
        .catch(error => {
          window.location.replace('/');
        });
    }
    else
      document.getElementById("deleteLink").style.visibility = "hidden";
      
  }

  onDelete() {
    axios.delete(`/api/firmpodcastsegments/${this.state.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
          this.props.history.push('/podcasts/firmpodcastsegments');
      })
      .catch(function (error) {
        console.log(error);
        if (error.response.status === 409) {
          sessionStorage.removeItem('token');
          window.location.replace('/login');
        }
        else
          alert('There was a problem deleting this firm podcast segement ' + error.response);
      });
  }

  findFirstError(formName, hasError) {
    const form = document.forms[formName];
    for (let i = 0; i < form.length; i++) {
      if (hasError(form[i].name)) {
        form[i].focus();
        break;
      }
    }
  }

  validateForm(errors) {
    this.findFirstError('simpleForm', (fieldName) => {
      return Boolean(errors[fieldName]);
    });
  }

  setUpdateResponse(response) {
    this.setState({ updateNoticeVisible: true });
  }

  toggleDeleteModal() {
    this.setState({
      deleteModal: !this.state.deleteModal
    });
  }

  touchAll(setTouched, errors) {
    setTouched({
      id: true,
      title: true,
      description: true,
      comment: true,
      segmentid: true,
      startson: true,
      endson: true,
      audiofile: true
    }
    );
    this.validateForm(errors);
  }

  handleChangeStart(startDate, endDate, setFieldValue) {
    // Is the new start date AFTER the end date? If so we need to bump up the enddate
    if (startDate.isAfter(endDate)) {
      setFieldValue('endson', startDate);
    }
  }

  handleChangeEnd(endDate, startDate, setFieldValue) {
    // Prevent end dates that go before the startdate
    if (endDate.isBefore(startDate)) {
      setFieldValue('endson', startDate);
    }
  }

  render() {
    return (
      <div className="animated fadein">
        <Formik
            enableReinitialize
            initialValues={{
              id: this.state.id, title: this.state.title, description: this.state.description, comment: this.state.comment, startson: this.state.startson, 
              endson: this.state.endson, audiofile: null, updateNoticeVisible: false, updateNoticeMessage: '', updateNoticeStyle: 'success'
            }}
            validate={validate(validationSchema)}
            onSubmit={onSubmit}
            render={
              ({
                values,
                errors,
                touched,
                status,
                dirty,
                handleChange,
                handleBlur,
                handleSubmit,
                isSubmitting,
                isValid,
                handleReset,
                setTouched,
                setFieldValue
              }) => (
                <Form onSubmit={handleSubmit} noValidate name='simpleForm'>
                  <Card>
                    <CardHeader><i className="fa fa-user" /><strong>Podcast Segment</strong>
                        <div className="card-header-actions">
                          <a id="deleteLink" href="#" onClick={this.toggleDeleteModal}><small className="text-muted">Delete</small></a>
                        </div>
                        <Modal isOpen={this.state.deleteModal} toggle={this.toggleDeleteModal} className={'modal-sm ' + this.props.className}>
                          <ModalHeader toggle={this.toggleDeleteModal}>Delete Podcast Segment</ModalHeader>
                          <ModalBody>Are you sure you wish to delete this podcast segment?</ModalBody>
                          <ModalFooter>
                            <Button color="primary" onClick={this.onDelete}>Delete</Button>{' '}
                            <Button color="secondary" onClick={this.toggleDeleteModal}>Cancel</Button>
                          </ModalFooter>
                        </Modal>
                    </CardHeader>
                    <CardBody>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="title">Title</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="title"
                              id="title"
                              placeholder="Segment title"
                              autoComplete="title"
                              valid={!errors.title}
                              invalid={touched.title && !!errors.title}
                              autoFocus={true}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.title}
                          />
                          <FormFeedback>{errors.title}</FormFeedback>
                          <FormText>The segment title will appear in the podcast listing.</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="description">Description</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="textarea"
                              name="description"
                              id="description"
                              placeholder="Description"
                              autoComplete="description"
                              rows="5"
                              valid={!errors.description}
                              invalid={touched.description && !!errors.description}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.description}
                          />
                          <FormFeedback>{errors.description}</FormFeedback>
                          <FormText>The segment description will be shown the listener of this podcast.</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="comment">Comment</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="comment"
                              id="comment"
                              placeholder="Comment"
                              autoComplete="comment"
                              valid={!errors.comment}
                              invalid={touched.comment && !!errors.comment}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.comment}
                          />
                          <FormFeedback>{errors.comment}</FormFeedback>
                          <FormText>The comment is not shown to the podcast listener and is used for your information only.</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="startson">Starting Date/Time</Label>
                        </Col>
                        <Col xs="12" md="4">
                            <DatePicker className="form-control is-valid"
                                name="startson"
                                id="startson"
                                valid={!errors.startson}
                                invalid={touched.startson && !!errors.startson}
                                onChange={e => { setFieldValue("startson", e); values.updateNoticeVisible = false; this.handleChangeStart(e,values.endson, setFieldValue); }}
                                dateFormat="MM-DD-YYYY h:mm A"
                                onBlur={handleBlur}
                                todayButton="Today"
                                showTimeSelect
                                timeFormat="h:mm A"
                                timeIntervals={15}
                                timeCaption="Time"
                                selected={values.startson}
                                selectsStart
                                startDate={values.startson}
                                endDate={values.endson}
                            />
                        <FormFeedback>{errors.startson}</FormFeedback>
                        </Col>
                          <Col md="2">
                            <Label htmlFor="startson">Ending Date/Time</Label>
                          </Col>
                          <Col xs="12" md="4">
                            <DatePicker className="form-control is-valid"
                                name="endson"
                                id="endson"
                                valid={!errors.endson}
                                invalid={touched.endson && !!errors.endson}
                                onChange={e => { setFieldValue("endson", e); values.updateNoticeVisible = false; this.handleChangeEnd(e, values.startson, setFieldValue); }}
                                dateFormat="MM-DD-YYYY h:mm A"
                                onBlur={handleBlur}
                                todayButton="Today"
                                showTimeSelect
                                timeFormat="h:mm A"
                                timeIntervals={15}
                                timeCaption="Time"
                                selected={values.endson}
                                selectsEnd
                                startDate={values.startson}
                                endDate={values.endson}
                            />
                            <FormFeedback>{errors.endson}</FormFeedback>
                          </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="audiofile">Audio</Label>
                        </Col>
                        <Col xs="12" md="10">
                            <Input type="file"
                                className="is-valid"
                                name="audiofile"
                                id="audiofile"
                                placeholder="Audio File"
                                autoComplete="audiofile"
                                valid={!errors.audiofile}
                                invalid={touched.audiofile && !!errors.audiofile}
                                onChange={(event) => { setFieldValue("audiofile", event.currentTarget.files[0]); values.updateNoticeVisible = false; }}
                          />
                          <FormFeedback>{errors.audiofile}</FormFeedback>
                          <FormText>The audio file must be in a mp3 format</FormText>
                        </Col>
                      </FormGroup>
                    </CardBody>
                    <CardFooter>
                      <FormGroup row>
                        <Col md="2">
                          <Button type="submit" color="primary" className="mr-1" disabled={isSubmitting || !isValid}>{isSubmitting ? 'Wait...' : 'Submit'}</Button>
                        </Col>
                        <Col md="10">
                          <Alert id="updateResponse" color={values.updateNoticeStyle} isOpen={values.updateNoticeVisible} >{values.updateNoticeMessage}</Alert>
                        </Col>
                      </FormGroup>
                    </CardFooter>
                  </Card>
                </Form>
              )}
        />
      </div>
    );
  }



}

export default withRouter(FirmPodcastSegment);
