import React, { Component }  from 'react';
import { Card, CardHeader, CardBody, Form, FormGroup, Col, InputGroup, Button, Input, InputGroupAddon } from 'reactstrap';
import BootstrapTable from 'react-bootstrap-table-next';
import paginationFactory from 'react-bootstrap-table2-paginator';

import axios from 'axios';
import { authHeader } from '../../_authHeader';

import { Link } from 'react-router-dom';

import 'react-bootstrap-table-next/dist/react-bootstrap-table2.min.css';
import 'react-bootstrap-table2-paginator/dist/react-bootstrap-table2-paginator.min.css';

class Clients extends Component {
  constructor(props) {
    super(props);
    this.state = {
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
          dataField: 'name',
          text: 'Name',
          sort: true,
          formatter: this.renderClientEditLink
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
          dataField: 'clientGroupName',
          text: 'Client Group',
          formatter: this.renderClientGroupEditLink
        },
        {
          dataField: 'emailAddress',
          text: 'Email'
        }
      ]    };

    this.handleSubmit = this.handleSubmit.bind(this);
    this.handleTableChange = this.handleTableChange.bind(this);
    this.handleSearchParameterChange = this.handleSearchParameterChange.bind(this);
    this.handleKeyPress = this.handleKeyPress.bind(this);
  }

  componentDidMount() {
    axios.get(`/api/clients?size=${this.state.sizePerPage}&page=1`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          this.setState({ clients: response.data });

          // We need total size as well
          axios.get(`/api/clients/count`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
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

  renderClientEditLink(cell, row, rowIndex) {
    return (
      <span>
        <Link to={{ pathname: `/directory/clients/${row.id}`, state: { prevpath: window.location.pathname } }}>{cell}</Link>
      </span>
    );
  }

  renderClientGroupEditLink(cell, row, rowIndex) {
    return (
      <span>
        <Link to={{ pathname: `/directory/clientgroups/${row.clientGroupId}`, state: { prevpath: window.location.pathname } }}>{cell}</Link>
      </span>
    );
  }

  searchClient = (page, sizePerPage) => {
    const inputValue = this.state.clientSearchTerm;

    // Get updated data
    axios.get(`/api/clients?name=${inputValue}&size=${sizePerPage}&page=${page}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          this.setState({ clients: response.data });

          // We need total size as well
          axios.get(`/api/clients/count?name=${inputValue}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
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

  handleTableChange = (type, { page, sizePerPage }) => {
    this.setState({ page: page });
    this.setState({ sizePerPage: sizePerPage });

    this.searchClient(page, sizePerPage);
  }

  handleSearch = event => {
    event.preventDefault();
    this.setState({ page: 1 });
    this.searchClient(1, this.state.sizePerPage);
  }

  handleSearchParameterChange = event => {
    this.setState({ clientSearchTerm: event.target.value });
  }

  handleSubmit = event => {
    event.preventDefault();
  }

  handleKeyPress = event => {
    if (event.key === 'Enter') {
      this.searchClient(this.state.page, this.state.sizePerPage);
    }
  }

  render() {
    const { totalSize, sizePerPage, page } = this.state;
    return (
      <div className="animated">
        <Card>
          <CardHeader>
            <i className="icon-menu" />Clients{' '}
            <div className="card-header-actions">
              <Link className="card-header-action" to={`/directory/clients/clientsimport`}><small className="text-muted">Import</small></Link>
            </div>
          </CardHeader>
          <CardBody>
            <Form className="form-horizontal" onSubmit={this.handleSubmit}>
              <FormGroup row>
                <Col md="12">
                  <InputGroup>
                    <Input type="text" id="searchParam" name="searchParam" placeholder="Search" onChange={this.handleSearchParameterChange} onKeyPress={this.handleKeyPress} />
                    <InputGroupAddon addonType="append">
                      <Button type="button" color="primary" id="search" onClick={this.handleSearch}>Search</Button>
                    </InputGroupAddon>
                  </InputGroup>
                </Col>
              </FormGroup>
            </Form>
            <BootstrapTable remote striped hover keyField='id' data={this.state.clients} columns={this.state.columns} noDataIndication="No clients for firm have been added" pagination={paginationFactory({ page, sizePerPage, totalSize } )} onTableChange={this.handleTableChange} />
          </CardBody>
        </Card>
      </div>
    );
  }

}


export default Clients;
