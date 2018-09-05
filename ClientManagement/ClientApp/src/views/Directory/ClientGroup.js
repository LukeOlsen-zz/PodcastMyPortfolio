import React, { Component } from 'react';
import { Alert, Button, Card, CardHeader, CardFooter, CardBody, Col, Form, FormFeedback, FormText, FormGroup, Label, Input, Modal, ModalBody, ModalFooter, ModalHeader, InputGroup, InputGroupAddon } from 'reactstrap';
import BootstrapTable from 'react-bootstrap-table-next';
import paginationFactory from 'react-bootstrap-table2-paginator';

import axios from 'axios';
import { authHeader } from '../../_authHeader';
import { Formik } from 'formik';
import * as Yup from 'yup';
import '../../ValidationForms.css';
import { withRouter } from 'react-router-dom';
import 'react-datepicker/dist/react-datepicker.css';
import { isNullOrUndefined, error } from 'util';
import { Link } from 'react-router-dom';
import 'react-bootstrap-table-next/dist/react-bootstrap-table2.min.css';
import 'react-bootstrap-table2-paginator/dist/react-bootstrap-table2-paginator.min.css';


const validationSchema = function (values) {
  return Yup.object().shape({
    name: Yup.string()
      .required('Name is required'),
    firmGroupId: Yup.string()
      .max(20,"Your firm's client group id must be 20 characters or less")
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
  const fd = new FormData();
  fd.append("name", values.name);
  fd.append("firmgroupid", values.firmGroupId);

  if (values.id === 0) {
    axios.post('/api/clientgroups/create', fd, {
      headers: { ...authHeader() }
    })
      .then(function (response) {
        setTimeout(() => {
          // At this point save was successful
          values.id = response.data.id;
          values.updateNoticeVisible = true;
          values.updateNoticeMessage = 'Group created';
          values.updateNoticeStyle = 'success';
          setSubmitting(false);
        }, 1000);
      })
      .catch(function (error) {
        if (error.response.status === 409) {
          sessionStorage.removeItem('token');
          window.location.replace('/login');
        }
        else {
          // Display error for user
          if (!isNullOrUndefined(error.response.data.field)) {
            setFieldError(error.response.data.field, error.response.data.message);
          }
          values.updateNoticeVisible = true;
          values.updateNoticeMessage = 'Group not created';
          values.updateNoticeStyle = 'danger';
          setSubmitting(false);
        }
      });
  }
  else {
    fd.append("id", values.id);
    axios.put('/api/clientgroups/update', fd, {
      headers: { ...authHeader() }
    })
      .then(function (response) {
        setTimeout(() => {
          // At this point save was successful
          values.updateNoticeVisible = true;
          values.updateNoticeMessage = 'Group updated';
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
          values.updateNoticeMessage = 'Group not updated';
          values.updateNoticeStyle = 'danger';
          setSubmitting(false);
        }
      });
  }
};

class ClientGroup extends Component {
  constructor(props) {
    super(props);
    this.state = {
      id: 0,
      firmGroupId: '',
      name: '',
      deleteModal: false,
      page: 1,
      totalSize: 0,
      sizePerPage: 10,
      clientSearchTerm: '',
      clients: [],
      columns: [
      {
        dataField: 'clientGroupId',
        hidden: true
      },
      {
        dataField: 'id',
        text: 'Id',
        hidden: true,
        headerStyle: (colum, colIndex) => {
          return { width: '50px' };
      }
      },
      {
        dataField: 'firmClientId',
        text: 'Id',
        sort: true,
        headerStyle: (colum, colIndex) => {
          return { width: '250px' };
        }
      },
      {
       dataField: 'name',
       text: 'Name',
       sort: true,
       formatter: this.renderClientEditLink
      },
      {
        dataField: 'emailAddress',
        text: 'Email'
      }
      ]
    };

    this.touchAll = this.touchAll.bind(this);
    this.toggleDeleteModal = this.toggleDeleteModal.bind(this);
    this.onDelete = this.onDelete.bind(this);

    this.handleClientSubmit = this.handleClientSubmit.bind(this);
    this.handleTableChange = this.handleTableChange.bind(this);
    this.handleSearchParameterChange = this.handleSearchParameterChange.bind(this);
    this.handleKeyPress = this.handleKeyPress.bind(this);
    this.handleSearch = this.handleSearch.bind(this);
  }

  componentDidMount() {
    const { match: { params } } = this.props;
    
    if (params.id !== '0') {
      axios.get(`/api/clientgroups/clientgroup/${params.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
        .then(response => {
          if (response.status === 200) {
            this.setState({ id: response.data.id });
            this.setState({ firmGroupId: response.data.firmGroupId });
            this.setState({ name: response.data.name });

            document.getElementById("deleteLink").style.visibility = "visible";
            document.getElementById("actionsOnClientsInGroup").style.visibility = "visible";

            axios.get(`/api/clients?clientgroupid=${params.id}&size=${this.state.sizePerPage}&page=1`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
              .then(response => {
                if (response.status === 200) {
                  this.setState({ clients: response.data });

                  // We need total size as well
                  axios.get(`/api/clients/count?clientgroupid=${params.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
                    .then(response => {
                      if (response.status === 200) {
                        this.setState({ totalSize: response.data });
                      }
                    });
                }
              })
              .catch(error => {
                window.location.replace('/');
              });


          }
        })
        .catch(error => {
          window.location.replace('/');
        });
    }
    else
    {
      document.getElementById("actionsOnClientsInGroup").style.visibility = "hidden";
      document.getElementById("deleteLink").style.visibility = "hidden";
    }
  }

  onDelete() {
    axios.delete(`/api/clientgroups/${this.state.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        this.props.history.push('/directory/clientgroups');
      })
      .catch(function (error) {
        console.log(error);
        if (error.response.status === 409) {
          sessionStorage.removeItem('token');
          window.location.replace('/login');
        }
        else
          alert('There was a problem deleting this group ' + error.response);
      });
  }

  renderClientEditLink(cell, row, rowIndex) {
    return (
      <span>
        <Link to={{ pathname: `/directory/clients/${row.id}`, state: { prevpath: window.location.pathname }}}>{cell}</Link>
      </span>
    );
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

  touchAll(setTouched, errors) {
    setTouched({
      id: true,
      name: true,
      firmGroupId: true
    }
    );
    this.validateForm(errors);
  }

  toggleDeleteModal() {
    this.setState({
      deleteModal: !this.state.deleteModal
    });
  }

  // Client routines

  handleClientSubmit = event => {
    event.preventDefault();
  }

  handleSearchParameterChange = event => {
    this.setState({ clientSearchTerm: event.target.value });
  }

  handleKeyPress = event => {
    if (event.key === 'Enter') {
      this.searchClients(this.state.page, this.state.sizePerPage);
    }
  }

  searchClients = (page, sizePerPage) => {
    const { match: { params } } = this.props;
    const inputValue = this.state.clientSearchTerm;

    if (!isNullOrUndefined(params.id) && params.id > 0)
    {
      axios.get(`/api/clients?clientgroupid=${params.id}&name=${inputValue}&size=${sizePerPage}&page=${page}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
        .then(response => {
          if (response.status === 200) {
            this.setState({ clients: response.data });

            // We need total size as well
            axios.get(`/api/clients/count?clientgroupid=${params.id}&name=${inputValue}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
              .then(response => {
                if (response.status === 200) {
                  this.setState({ totalSize: response.data });
                }
              });
          }
        })
        .catch(error => {
          window.location.replace('/');
        });
    }
  }

  handleTableChange = (type, { page, sizePerPage }) => {
    this.setState({ page: page });
    this.setState({ sizePerPage: sizePerPage });

    this.searchClients(page, sizePerPage);
  }

  handleSearch = event => {
    event.preventDefault();
    this.setState({ page: 1 });
    this.searchClients(1, this.state.sizePerPage);
  }


  render() {
    const { totalSize, sizePerPage, page } = this.state;
    return (
      <div className="animated fadein">
        <Formik
            enableReinitialize
            initialValues={{
              id: this.state.id, name: this.state.name, firmGroupId: this.state.firmGroupId, updateNoticeVisible: false, updateNoticeMessage: '', updateNoticeStyle: 'success'
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
                    <CardHeader><i className="fa fa-user" /><strong>Client Group</strong>
                      <div className="card-header-actions">
                        <a id="deleteLink" href="#" onClick={this.toggleDeleteModal}><small className="text-muted">Delete</small></a>
                      </div>
                      <Modal isOpen={this.state.deleteModal} toggle={this.toggleDeleteModal} className={'modal-sm ' + this.props.className}>
                        <ModalHeader toggle={this.toggleDeleteModal}>Delete Client Group</ModalHeader>
                        <ModalBody>Are you sure you wish to delete this client group? NOTE: You will not be able to delete a client group without first deleting all the group's clients first.</ModalBody>
                        <ModalFooter>
                          <Button color="primary" onClick={this.onDelete}>Delete</Button>{' '}
                          <Button color="secondary" onClick={this.toggleDeleteModal}>Cancel</Button>
                        </ModalFooter>
                      </Modal>
                    </CardHeader>
                    <CardBody>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="name">Name</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="name"
                              id="name"
                              placeholder="Client group name"
                              autoComplete="name"
                              valid={!errors.name}
                              invalid={touched.name && !!errors.name}
                              autoFocus={true}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.name}
                          />
                          <FormFeedback>{errors.name}</FormFeedback>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                            <Label htmlFor="firmGroupId">Firm Group Id</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="firmGroupId"
                              id="firmGroupId"
                              placeholder="Firm Group Id"
                              autoComplete="firmGroupId"
                              valid={!errors.firmGroupId}
                              invalid={touched.firmGroupId && !!errors.firmGroupId}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.firmGroupId}
                          />
                          <FormFeedback>{errors.firmGroupId}</FormFeedback>
                          <FormText>The Firm Group Id is the id assigned by the firm.</FormText>
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
        <Card>
          <CardHeader>
            <i className="icon-menu" />Clients In This Group{' '}
            <div className="card-header-actions" id="actionsOnClientsInGroup">
            </div>
          </CardHeader>
          <CardBody>
            <Form className="form-horizontal" onSubmit={this.handleClientSubmit}>
              <FormGroup row>
                <Col md="12">
                  <InputGroup>
                    <Input type="text" id="searchParam" name="searchParam" placeholder="Search Within This Group" onChange={this.handleSearchParameterChange} onKeyPress={this.handleKeyPress} />
                    <InputGroupAddon addonType="append">
                      <Button type="button" color="primary" id="search" onClick={this.handleSearch}>Search</Button>
                    </InputGroupAddon>
                  </InputGroup>
                </Col>
              </FormGroup>
            </Form>
            <BootstrapTable remote striped hover keyField='id' data={this.state.clients} columns={this.state.columns} noDataIndication="No clients for this group have been found" pagination={paginationFactory({ page, sizePerPage, totalSize })} onTableChange={this.handleTableChange} />
          </CardBody>
        </Card>
      </div>
    );
  }



}

export default withRouter(ClientGroup);

